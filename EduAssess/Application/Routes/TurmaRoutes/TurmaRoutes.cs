using EduAssess.Application.DTOs.Turma;
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
                async (TurmaRequest req, AppDbContext context) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);
                    var existingTurma = await context.Turmas.FirstOrDefaultAsync(t => t.Nome == req.nome);
                    if (existingTurma != null)
                        return Results.BadRequest(new { message = "Turma já existente" });
                    var turma = new TurmaModel(req.nome, req.professorId, req.cursoId);
                    await context.Turmas.AddAsync(turma);
                    await context.SaveChangesAsync();
                    return Results.Ok(new { message = "Turma criada com sucesso", turmaId = turma.Id });
                }).RequireAuthorization("Gestao");

            route.MapGet("get/{id:guid}",
                async (Guid id, AppDbContext context) =>
                {
                    var turma = await context.Turmas.FindAsync(id);
                    if (turma == null || !turma.Ativo)
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
        }
    }
}
