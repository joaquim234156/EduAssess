
using System;
using System.Linq;
using System.Security.Claims;
using EduAssess.Application.DTOs.Questao;
using EduAssess.Data;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace EduAssess.Application.Routes
{
    public static class QuestaoRoute
    {
        public static void QuestaoRoutes(this WebApplication app)
        {
            var route = app.MapGroup("questoes")
                .WithTags("Questões");

            route.MapPost("create",
                async (
                    QuestaoRequest req,
                    AppDbContext context,
                    ClaimsPrincipal usuario) =>
                {
                    // 1. Validar os dados recebidos.
                    if (!MiniValidation.MiniValidator.TryValidate(
                        req, out var errors))
                    {
                        return Results.BadRequest(errors);
                    }

                    // 2. Verificar o ID da disciplina.
                    if (req.disciplinaId is not Guid disciplinaId ||
                        disciplinaId == Guid.Empty)
                    {
                        return Results.BadRequest(new
                        {
                            message = "Escolha uma disciplina válida."
                        });
                    }

                    // 3. Obter o ID do professor autenticado.
                    var idUsuario = usuario.FindFirst(
                        ClaimTypes.NameIdentifier)?.Value;

                    if (!Guid.TryParse(idUsuario, out var professorId))
                    {
                        return Results.Unauthorized();
                    }

                    // 4. Verificar se a disciplina existe e está ativa.
                    var disciplina = await context.Disciplinas
                        .FirstOrDefaultAsync(d =>
                            d.Id == disciplinaId && d.Ativa);

                    if (disciplina == null)
                    {
                        return Results.NotFound(new
                        {
                            message = "Disciplina não encontrada."
                        });
                    }

                    // 5. Impedir o professor de utilizar
                    // uma disciplina atribuída a outra pessoa.
                    if (disciplina.ProfessorId != professorId)
                    {
                        return Results.Forbid();
                    }

                    // 6. Garantir que a resposta correta
                    // corresponda a uma das cinco alternativas.
                    var alternativas = new[]
                    {
                        req.alternativa1,
                        req.alternativa2,
                        req.alternativa3,
                        req.alternativa4,
                        req.alternativa5
                    };

                    var respostaExiste = alternativas.Any(a =>
                        string.Equals(
                            a.Trim(),
                            req.alternativaCorreta.Trim(),
                            StringComparison.OrdinalIgnoreCase));

                    if (!respostaExiste)
                    {
                        return Results.BadRequest(new
                        {
                            message =
                                "A resposta correta deve corresponder " +
                                "a uma das cinco alternativas."
                        });
                    }

                    // 7. Criar a entidade.
                    var questao = new QuestaoModel(
                        professorId,
                        disciplinaId,
                        req.enunciado,
                        req.alternativaCorreta,
                        req.alternativa1,
                        req.alternativa2,
                        req.alternativa3,
                        req.alternativa4,
                        req.alternativa5,
                        req.assunto,
                        req.explicacao,
                        req.dificuldade!.Value);

                    // 8. Persistir a questão.
                    await context.Questoes.AddAsync(questao);
                    await context.SaveChangesAsync();

                    // 9. Retornar a questão cadastrada.
                    var response = new QuestaoResponseDTO(
                        questao.Id,
                        questao.DisciplinaId,
                        questao.ProfessorId,
                        questao.Enunciado,
                        questao.Alternativa1,
                        questao.Alternativa2,
                        questao.Alternativa3,
                        questao.Alternativa4,
                        questao.Alternativa5,
                        questao.AlternativaCorreta,
                        questao.Assunto,
                        questao.Explicacao,
                        questao.Dificuldade,
                        questao.Ativa);

                    return Results.Ok(new
                    {
                        message = "Questão cadastrada com sucesso!",
                        resposta = response
                    });
                })
                .RequireAuthorization("Professor");
            route.MapGet("get{id:guid}",
            async (Guid id, AppDbContext context) =>
                {
                    var questao = await context.Questoes.FindAsync(id);
                    if (questao == null || !questao.Ativa)
                    {
                        return Results.NotFound(new{message = "A questão desejada não existe"});
                    }

                    var response = new QuestaoResponseDTO(
                        questao.Id,
                        questao.DisciplinaId,
                        questao.ProfessorId,
                        questao.Enunciado,
                        questao.Alternativa1,
                        questao.Alternativa2,
                        questao.Alternativa3,
                        questao.Alternativa4,
                        questao.Alternativa5,
                        questao.AlternativaCorreta,
                        questao.Explicacao,
                        questao.Assunto,
                        questao.Dificuldade,
                        questao.Ativa
                    );
                    return Results.Ok(new
                    {
                        message = "Curso encontrado!!",
                        resposta = response
                    });
                });
            route.MapPut("update{id:guid}",
            async (Guid id,QuestaoRequest req, AppDbContext context, ClaimsPrincipal usuario) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                {
                    return Results.BadRequest(errors);
                }


                if (req.disciplinaId is not Guid disciplinaId ||
                        disciplinaId == Guid.Empty)
                    {
                        return Results.BadRequest(new
                        {
                            message = "Escolha uma disciplina válida."
                        });
                    }

                var idUsuario = usuario.FindFirst(
                        ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(idUsuario, out var professorId))
                    {
                        return Results.Unauthorized();
                    }

                    // 4. Verificar se a disciplina existe e está ativa.
                    var disciplina = await context.Disciplinas
                        .FirstOrDefaultAsync(d =>
                            d.Id == disciplinaId && d.Ativa);

                    if (disciplina == null)
                    {
                        return Results.NotFound(new
                        {
                            message = "Disciplina não encontrada."
                        });
                    }

                    // 5. Impedir o professor de utilizar
                    // uma disciplina atribuída a outra pessoa.
                    if (disciplina.ProfessorId != professorId)
                    {
                        return Results.Forbid();
                    }

                    // 6. Garantir que a resposta correta
                    // corresponda a uma das cinco alternativas.
                    var alternativas = new[]
                    {
                        req.alternativa1,
                        req.alternativa2,
                        req.alternativa3,
                        req.alternativa4,
                        req.alternativa5
                    };

                    var respostaExiste = alternativas.Any(a =>
                        string.Equals(
                            a.Trim(),
                            req.alternativaCorreta.Trim(),
                            StringComparison.OrdinalIgnoreCase));

                    if (!respostaExiste)
                    {
                        return Results.BadRequest(new
                        {
                            message =
                                "A resposta correta deve corresponder " +
                                "a uma das cinco alternativas."
                        });
                    }

                var questao = await context.Questoes.FindAsync(id);
                if (questao == null || !questao.Ativa)
                {
                    return Results.NotFound(new{message="A questão solicitada não existe no banco de dados"});
                }

                questao.UpdateQuestion(
                    disciplinaId,
                    req.enunciado,
                    req.alternativaCorreta,
                    req.alternativa1,
                    req.alternativa2,
                    req.alternativa3,
                    req.alternativa4,
                    req.alternativa5,
                    req.assunto,
                    req.explicacao,
                    req.dificuldade!.Value);

                var response = new QuestaoResponseDTO(
                        questao.Id,
                        questao.DisciplinaId,
                        questao.ProfessorId,
                        questao.Enunciado,
                        questao.Alternativa1,
                        questao.Alternativa2,
                        questao.Alternativa3,
                        questao.Alternativa4,
                        questao.Alternativa5,
                        questao.AlternativaCorreta,
                        questao.Assunto,
                        questao.Explicacao,
                        questao.Dificuldade,
                        questao.Ativa);

                await context.SaveChangesAsync();

                return Results.Ok(new
                {
                    message = "Questão atualizada com sucesso!!!",
                    resposta = response
                });
            }).RequireAuthorization("Professor");
            route.MapDelete("delete{id:guid}",
            async(Guid id, AppDbContext context) =>
            {
                var questao = await context.Questoes.FindAsync(id);
                if(questao == null || !questao.Ativa)
                {
                    return Results.NotFound(new {message = "Questão não encontrada"});
                }

                questao.Disable();
                await context.SaveChangesAsync();
                return Results.Ok(new{message = "Questão apagada com sucesso"});
            }).RequireAuthorization("Professor");
            route.MapGet("get",
            async(Guid? disciplinaId, string? assunto, DificuldadeQuestao? dificuldade, AppDbContext context) =>
            {
                var consulta =  context.Questoes.AsNoTracking().Where(q => q.Ativa);

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

                var questoes = await consulta.OrderBy(q => q.Assunto).Select(q=> new QuestaoResponseDTO(
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
                        q.Ativa)).ToListAsync();

                return Results.Ok(questoes);
            }).RequireAuthorization("Docente");
        }
    }
}
