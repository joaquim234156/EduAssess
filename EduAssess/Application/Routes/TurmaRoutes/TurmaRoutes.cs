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
