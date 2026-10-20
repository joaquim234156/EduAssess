using EduAssess.Application.DTOs.Turma;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Routes
{
    public static class TurmaRoute
    {
        public static void TurmaRoutes(this WebApplication app)
        {
            var route = app.MapGroup("turmas").WithTags("Turmas");
            route.MapPost("create",
                async (TurmaRequest req, AppDbContext context, ITurmaService turmaService) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    // Required não rejeita Guid.Empty; validar os IDs antes de consultar ou persistir a turma.
                    if (req.professorId == Guid.Empty || req.cursoId == Guid.Empty)
                        return Results.BadRequest(new { message = "Informe IDs válidos para professor e curso." });

                    // Garante que o professor selecionado existe, está ativo e tem a role correta.
                    var professorValido = await context.Users.AnyAsync(user =>
                        user.Id == req.professorId && user.Ativa && user.Role == UserRole.Professor);
                    if (!professorValido)
                        return Results.BadRequest(new { message = "O ID informado não pertence a um professor ativo." });

                    // Evita criar turmas vinculadas a cursos inexistentes ou desativados.
                    var cursoAtivo = await context.Cursos.AnyAsync(curso =>
                        curso.Id == req.cursoId && curso.Ativo);
                    if (!cursoAtivo)
                        return Results.BadRequest(new { message = "O curso informado não existe ou está inativo." });

                    var existingTurma = await context.Turmas.FirstOrDefaultAsync(t => t.Nome == req.nome);
                    if (existingTurma != null)
                        return Results.BadRequest(new { message = "Turma já existente" });

                    var turma = await turmaService.CriarTurmaAsync(req);
                    return Results.Ok(new { message = "Turma criada com sucesso", turmaId = turma.Id });
                }).RequireAuthorization("Admin");

            route.MapGet("list",
                async (ITurmaService turmaService) =>
                {
                    var turmas = await turmaService.ListarTurmasAsync();
                    return Results.Ok(new { message = "Lista de turmas", turmas });
                }).RequireAuthorization("Admin");

            route.MapGet("get/{id:guid}",
                async (Guid id, AppDbContext context, ITurmaService turmaService) =>
                {
                    var turma = await turmaService.ObterTurmaPorIdAsync(id);
                    if (turma == null)
                        return Results.NotFound(new
                        {
                            message = "Turma não existente"
                        });

                    return Results.Ok(new
                    {
                        message = "Turma Encontrada",
                        turma = new
                        {
                            turma.Id,
                            turma.Nome,
                            turma.CursoId,
                            turma.ProfessorId,
                            turma.Periodo,
                        }
                    });
                });

            route.MapPut("update/{id:guid}",
                async (Guid id, TurmaRequestPut req, AppDbContext context, ITurmaService turmaService) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    // Required não rejeita Guid.Empty; validar os IDs antes de atualizar as referências.
                    if (req.professorId == Guid.Empty || req.cursoId == Guid.Empty)
                        return Results.BadRequest(new { message = "Informe IDs válidos para professor e curso." });

                    // Mantém o vínculo apenas com um professor existente, ativo e com a role Professor.
                    var professorValido = await context.Users.AnyAsync(user =>
                        user.Id == req.professorId && user.Ativa && user.Role == UserRole.Professor);
                    if (!professorValido)
                        return Results.BadRequest(new { message = "O ID informado não pertence a um professor ativo." });

                    // Impede trocar a turma para um curso inexistente ou desativado.
                    var cursoAtivo = await context.Cursos.AnyAsync(curso =>
                        curso.Id == req.cursoId && curso.Ativo);
                    if (!cursoAtivo)
                        return Results.BadRequest(new { message = "O curso informado não existe ou está inativo." });

                    var existingTurma = await context.Turmas.FirstOrDefaultAsync(t => t.Id == id);
                    if (existingTurma == null)
                        return Results.NotFound(new { message = "Turma não existente" });
                    var updatedTurma = await turmaService.AtualizarTurmaAsync(id, req);
                    return Results.Ok(new { message = "Turma atualizada com sucesso", turmaId = updatedTurma.Id });
                }).RequireAuthorization("Admin");

            route.MapDelete("delete/{id:guid}",
                async (Guid id, AppDbContext context, ITurmaService turmaService) =>
                {
                    var existingTurma = await context.Turmas.FirstOrDefaultAsync(t => t.Id == id);
                    if (existingTurma == null)
                        return Results.NotFound(new { message = "Turma não existente" });
                    var deleted = await turmaService.DeletarTurmaAsync(id);
                    if (!deleted)
                        return Results.BadRequest(new { message = "Erro ao deletar a turma" });
                    return Results.Ok(new { message = "Turma deletada com sucesso" });
                }).RequireAuthorization("Admin");
        }
    }
}
