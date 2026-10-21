using EduAssess.Application.DTOs.Nota;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduAssess.Application.Routes
{
    public static class NotaRoute
    {
        public static void NotaRoutes(this WebApplication app)
        {
            var route = app.MapGroup("notas").WithTags("Notas");

            route.MapPost("registrar", async (NotaRequest req, ClaimsPrincipal user, AppDbContext context, INotaService notaService) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);

                if (req.idMatricula == Guid.Empty || req.idDisciplina == Guid.Empty)
                    return Results.BadRequest(new { message = "Informe IDs válidos para matrícula e disciplina." });

                // Vincula a nota ao professor autenticado, sem confiar em um idUsuario enviado no JSON.
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userId, out var professorId))
                    return Results.Unauthorized();

                // Confirma o perfil ativo no banco antes de registrar o autor da nota.
                var professorAtivo = await context.Users.AnyAsync(u =>
                    u.Id == professorId && u.Ativa && u.Role == UserRole.Professor);
                if (!professorAtivo)
                    return Results.Forbid();

                // Professor só lança nota em matrícula ativa de uma turma que lhe foi atribuída.
                var matriculaDoProfessor = await context.Matriculas.AnyAsync(m =>
                    m.Id == req.idMatricula && m.Ativa && m.Turma.ProfessorId == professorId);
                if (!matriculaDoProfessor)
                    return Results.BadRequest(new { message = "A matrícula não existe, está inativa ou não pertence a uma turma do professor." });

                // Impede que o professor registre nota para disciplina de outro professor.
                var disciplinaDoProfessor = await context.Disciplinas.AnyAsync(d =>
                    d.Id == req.idDisciplina && d.Ativa && d.ProfessorId == professorId);
                if (!disciplinaDoProfessor)
                    return Results.BadRequest(new { message = "A disciplina não existe, está inativa ou não pertence ao professor autenticado." });

                var resultado = await notaService.RegistrarNotaAsync(req, professorId);
                return Results.Ok(resultado);
            }).RequireAuthorization("Professor");

            route.MapGet("listar", async (INotaService notaService) =>
            {
                var notas = await notaService.ListarTodasAsync();
                return Results.Ok(notas);
            }).RequireAuthorization("Professor");

            route.MapGet("aluno/{idMatricula:guid}", async (Guid idMatricula, INotaService notaService) =>
            {
                var notasDoAluno = await notaService.ObterNotasPorMatriculaAsync(idMatricula);

                if (!notasDoAluno.Any())
                    return Results.NotFound(new { mensagem = "Nenhuma nota encontrada para este aluno." });

                return Results.Ok(notasDoAluno);
            }).RequireAuthorization("Professor");

            route.MapPut("atualizar/{id:guid}", async (Guid id, NotaRequest req, INotaService notaService) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);
                var notaAtualizada = await notaService.AtualizarNotaAsync(id, req);
                return Results.Ok(notaAtualizada);
            }).RequireAuthorization("Professor");

            route.MapDelete("deletar/{id:guid}", async (Guid id, INotaService notaService) =>
            {
                var sucesso = await notaService.DeletarNotaAsync(id);
                if (!sucesso)
                    return Results.NotFound(new { mensagem = "Nota não encontrada ou já deletada." });
                return Results.Ok(new { mensagem = "Nota deletada com sucesso." });
            }).RequireAuthorization("Professor");
        }
    }
}
