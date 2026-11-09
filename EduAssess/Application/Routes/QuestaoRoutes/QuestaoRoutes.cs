using System;
using System.Linq;
using System.Security.Claims;
using EduAssess.Application.DTOs.Questao;
using EduAssess.Application.Interfaces;
using EduAssess.Application.Services;
using EduAssess.Data;
using EduAssess.Models.Local;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Routes
{
    public static class QuestaoRoute
    {
        // ALTERADO: criei este método para montar a resposta em um só lugar.
        // Antes o mesmo bloco "new QuestaoResponseDTO(...)" estava copiado 4 vezes
        // e uma das cópias tinha Assunto e Explicacao trocados (bug em GET get{id}).
        private static QuestaoResponseDTO ToResponse(QuestaoModel q) => new(
            q.Id,
            q.DisciplinaId,
            q.ProfessorId,
            q.Enunciado,
            q.Alternativa1,
            q.Alternativa2,
            q.Alternativa3,
            q.Alternativa4,
            q.Alternativa5,
            q.AlternativaCorreta,
            q.Assunto,
            q.Explicacao,
            q.Dificuldade,
            q.Ativa);

        // NOVO: gestor e administrador podem trabalhar em QUALQUER disciplina e mexer em questões
        // de qualquer autor; o professor só nas dele. Centralizei a regra aqui.
        private static bool EhGestao(ClaimsPrincipal u) =>
            u.IsInRole(nameof(UserRole.Admin)) || u.IsInRole(nameof(UserRole.Gestor));

        // NOVO: confere se o texto da resposta correta é igual a uma das 5 alternativas.
        private static bool RespostaCorretaExiste(QuestaoRequest req)
        {
            var alternativas = new[]
            {
                req.alternativa1, req.alternativa2, req.alternativa3,
                req.alternativa4, req.alternativa5
            };

            return alternativas.Any(a =>
                string.Equals(a.Trim(), req.alternativaCorreta.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        public static void QuestaoRoutes(this WebApplication app)
        {
            var route = app.MapGroup("questoes")
                .WithTags("Questões");

            // ALTERADO: as rotas de escrita (create, gerar-ia, update, delete) exigiam o perfil
            // "Professor". Agora usam "Docencia" (Admin, Gestor e Professor), conforme pedido.
            // Dentro de cada rota, a regra de posse continua: professor só mexe no que é dele.

            // ───────────── Criar uma questão (manual) ─────────────
            route.MapPost("create",
                async (
                    QuestaoRequest req,
                    AppDbContext context,
                    ClaimsPrincipal usuario) =>
                {
                    // 1. Validar os dados recebidos.
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    {
                        return Results.BadRequest(errors);
                    }

                    // 2. Verificar o ID da disciplina.
                    if (req.disciplinaId is not Guid disciplinaId ||
                        disciplinaId == Guid.Empty)
                    {
                        return Results.BadRequest(new { message = "Escolha uma disciplina válida." });
                    }

                    // 3. Obter o ID do professor autenticado.
                    var idUsuario = usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!Guid.TryParse(idUsuario, out var professorId))
                    {
                        return Results.Unauthorized();
                    }

                    // 4. Verificar se a disciplina existe e está ativa.
                    var disciplina = await context.Disciplinas
                        .FirstOrDefaultAsync(d => d.Id == disciplinaId && d.Ativa);
                    if (disciplina == null)
                    {
                        return Results.NotFound(new { message = "Disciplina não encontrada." });
                    }

                    // 5. Impedir o professor de usar uma disciplina de outra pessoa.
                    if (!EhGestao(usuario) && disciplina.ProfessorId != professorId)
                    {
                        return Results.Forbid();
                    }

                    // 6. A resposta correta precisa ser uma das cinco alternativas.
                    if (!RespostaCorretaExiste(req))
                    {
                        return Results.BadRequest(new
                        {
                            message = "A resposta correta deve corresponder a uma das cinco alternativas."
                        });
                    }

                    // 7. Criar e salvar.
                    var questao = new QuestaoModel(
                        professorId, disciplinaId, req.enunciado, req.alternativaCorreta,
                        req.alternativa1, req.alternativa2, req.alternativa3,
                        req.alternativa4, req.alternativa5,
                        req.assunto, req.explicacao, req.dificuldade!.Value);

                    await context.Questoes.AddAsync(questao);
                    await context.SaveChangesAsync();

                    return Results.Ok(new
                    {
                        message = "Questão cadastrada com sucesso!",
                        resposta = ToResponse(questao)
                    });
                })
                .RequireAuthorization("Docencia");

            // ───────────── NOVO: gerar questões com a IA (Gemini) ─────────────
            // Fluxo igual ao do PreparaEdu: o professor pede, REVISA o que a IA escreveu e só
            // então salva. Por isso, por padrão, a rota só devolve a prévia (nada vai ao banco).
            // Com ?salvar=true as questões geradas já são gravadas direto.
            route.MapPost("gerar-ia",
                async (
                    GerarQuestoesRequest req,
                    AppDbContext context,
                    IIaQuestaoService ia,
                    ClaimsPrincipal usuario,
                    bool? salvar) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    {
                        return Results.BadRequest(errors);
                    }

                    var idUsuario = usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!Guid.TryParse(idUsuario, out var professorId))
                    {
                        return Results.Unauthorized();
                    }

                    var disciplinaId = req.disciplinaId!.Value;
                    var disciplina = await context.Disciplinas
                        .FirstOrDefaultAsync(d => d.Id == disciplinaId && d.Ativa);
                    if (disciplina == null)
                    {
                        return Results.NotFound(new { message = "Disciplina não encontrada." });
                    }

                    // Só o professor responsável pela disciplina pode gerar questões para ela.
                    if (!EhGestao(usuario) && disciplina.ProfessorId != professorId)
                    {
                        return Results.Forbid();
                    }

                    var assunto = req.assunto.Trim();

                    // Enunciados que o professor já tem nesse assunto: a IA não deve repeti-los.
                    var existentes = await context.Questoes.AsNoTracking()
                        .Where(q => q.Ativa && q.DisciplinaId == disciplinaId && q.Assunto == assunto)
                        .Take(15)
                        .Select(q => q.Enunciado)
                        .ToListAsync();

                    List<QuestaoRequest> geradas;
                    try
                    {
                        geradas = await ia.GerarQuestoesAsync(
                            disciplinaId, disciplina.Nome, assunto,
                            req.dificuldade!.Value, req.quantidade, existentes, req.recomendacoes);
                    }
                    catch (IaQuestaoException ex)
                    {
                        // A exceção própria diz qual status devolver (429, 503, 502...).
                        return Results.Json(new { message = ex.Message }, statusCode: ex.StatusCode);
                    }

                    if (salvar != true)
                    {
                        return Results.Ok(new
                        {
                            message = $"{geradas.Count} questão(ões) gerada(s). Revise antes de salvar.",
                            salvas = false,
                            questoes = geradas
                        });
                    }

                    var modelos = geradas.Select(g => new QuestaoModel(
                        professorId, disciplinaId, g.enunciado, g.alternativaCorreta,
                        g.alternativa1, g.alternativa2, g.alternativa3,
                        g.alternativa4, g.alternativa5,
                        g.assunto, g.explicacao, g.dificuldade!.Value)).ToList();

                    await context.Questoes.AddRangeAsync(modelos);
                    await context.SaveChangesAsync();

                    return Results.Ok(new
                    {
                        message = $"{modelos.Count} questão(ões) gerada(s) e salva(s)!",
                        salvas = true,
                        questoes = modelos.Select(ToResponse).ToList()
                    });
                })
                .RequireAuthorization("Docencia");

            // ───────────── Buscar uma questão ─────────────
            // ALTERADO: esta rota não tinha RequireAuthorization, ou seja, qualquer pessoa na
            // internet (sem login) conseguia ler a questão COM a resposta correta. Agora exige
            // perfil docente (Admin, Gestor ou Professor).
            route.MapGet("get{id:guid}",
                async (Guid id, AppDbContext context) =>
                {
                    var questao = await context.Questoes.AsNoTracking()
                        .FirstOrDefaultAsync(q => q.Id == id);
                    if (questao == null || !questao.Ativa)
                    {
                        return Results.NotFound(new { message = "A questão desejada não existe" });
                    }

                    return Results.Ok(new
                    {
                        message = "Questão encontrada!",   // antes dizia "Curso encontrado!!"
                        resposta = ToResponse(questao)
                    });
                })
                .RequireAuthorization("Docencia");

            // ───────────── Atualizar uma questão ─────────────
            route.MapPut("update{id:guid}",
                async (Guid id, QuestaoRequest req, AppDbContext context, ClaimsPrincipal usuario) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    {
                        return Results.BadRequest(errors);
                    }

                    if (req.disciplinaId is not Guid disciplinaId || disciplinaId == Guid.Empty)
                    {
                        return Results.BadRequest(new { message = "Escolha uma disciplina válida." });
                    }

                    var idUsuario = usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!Guid.TryParse(idUsuario, out var professorId))
                    {
                        return Results.Unauthorized();
                    }

                    var disciplina = await context.Disciplinas
                        .FirstOrDefaultAsync(d => d.Id == disciplinaId && d.Ativa);
                    if (disciplina == null)
                    {
                        return Results.NotFound(new { message = "Disciplina não encontrada." });
                    }

                    if (!EhGestao(usuario) && disciplina.ProfessorId != professorId)
                    {
                        return Results.Forbid();
                    }

                    if (!RespostaCorretaExiste(req))
                    {
                        return Results.BadRequest(new
                        {
                            message = "A resposta correta deve corresponder a uma das cinco alternativas."
                        });
                    }

                    var questao = await context.Questoes.FindAsync(id);
                    if (questao == null || !questao.Ativa)
                    {
                        return Results.NotFound(new { message = "A questão solicitada não existe no banco de dados" });
                    }

                    // NOVO: antes qualquer professor conseguia editar a questão de outro.
                    if (!EhGestao(usuario) && questao.ProfessorId != professorId)
                    {
                        return Results.Forbid();
                    }

                    questao.UpdateQuestion(
                        disciplinaId, req.enunciado, req.alternativaCorreta,
                        req.alternativa1, req.alternativa2, req.alternativa3,
                        req.alternativa4, req.alternativa5,
                        req.assunto, req.explicacao, req.dificuldade!.Value);

                    // ALTERADO: salvamos ANTES de montar a resposta (antes era depois).
                    await context.SaveChangesAsync();

                    return Results.Ok(new
                    {
                        message = "Questão atualizada com sucesso!!!",
                        resposta = ToResponse(questao)
                    });
                })
                .RequireAuthorization("Docencia");

            // ───────────── Apagar (desativar) uma questão ─────────────
            route.MapDelete("delete{id:guid}",
                async (Guid id, AppDbContext context, ClaimsPrincipal usuario) =>
                {
                    var idUsuario = usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!Guid.TryParse(idUsuario, out var professorId))
                    {
                        return Results.Unauthorized();
                    }

                    var questao = await context.Questoes.FindAsync(id);
                    if (questao == null || !questao.Ativa)
                    {
                        return Results.NotFound(new { message = "Questão não encontrada" });
                    }

                    // NOVO: só o autor da questão pode apagá-la.
                    if (!EhGestao(usuario) && questao.ProfessorId != professorId)
                    {
                        return Results.Forbid();
                    }

                    questao.Disable();
                    await context.SaveChangesAsync();
                    return Results.Ok(new { message = "Questão apagada com sucesso" });
                })
                .RequireAuthorization("Docencia");

            // ───────────── Listar questões ─────────────
            // ALTERADO: a policy era "Docente", que NÃO existe no Program.cs (a correta é
            // "Docencia"). Isso fazia a rota quebrar em tempo de execução.
            route.MapGet("get",
                async (Guid? disciplinaId, string? assunto, DificuldadeQuestao? dificuldade, AppDbContext context) =>
                {
                    var consulta = context.Questoes.AsNoTracking().Where(q => q.Ativa);

                    if (disciplinaId.HasValue)
                    {
                        consulta = consulta.Where(q => q.DisciplinaId == disciplinaId.Value);
                    }
                    if (!string.IsNullOrWhiteSpace(assunto))
                    {
                        consulta = consulta.Where(q => q.Assunto.Contains(assunto.Trim()));
                    }
                    if (dificuldade.HasValue)
                    {
                        consulta = consulta.Where(q => q.Dificuldade == dificuldade.Value);
                    }

                    var questoes = await consulta.OrderBy(q => q.Assunto).ToListAsync();
                    return Results.Ok(questoes.Select(ToResponse).ToList());
                })
                .RequireAuthorization("Docencia");
        }
    }
}
