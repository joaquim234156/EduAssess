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

                    if (!await context.Cursos.AnyAsync(c => c.Id == req.cursoId && c.Ativo))
                        return Results.BadRequest(new { message = "Selecione um curso ativo." });
                    if (!await context.Users.AnyAsync(u => u.Id == req.professorId && u.Ativa && u.Role == UserRole.Professor))
                        return Results.BadRequest(new { message = "Selecione um professor ativo." });

                    var nome = req.nome.Trim();
                    if (await context.Turmas.AnyAsync(t => t.Ativo && t.Nome == nome))
                        return Results.BadRequest(new { message = "Turma já existente" });

                    var turma = new TurmaModel(nome, req.professorId, req.cursoId);
                    await context.Turmas.AddAsync(turma);
                    await context.SaveChangesAsync();
                    return Results.Ok(new { message = "Turma criada com sucesso", turmaId = turma.Id });
                }).RequireAuthorization("Gestao");

            route.MapGet("list",
                async (AppDbContext context) =>
                {
                    var turmas = await context.Turmas.AsNoTracking()
                        .Where(t => t.Ativo)
                        .OrderBy(t => t.Nome)
                        .Select(t => new TurmaResponseDTO(t.Id, t.Nome, t.CursoId, t.ProfessorId, t.Periodo, t.Ativo))
                        .ToListAsync();
                    return Results.Ok(turmas);
                }).RequireAuthorization("Gestao");

            route.MapGet("get/{id:guid}",
                async (Guid id, AppDbContext context) =>
                {
                    var turma = await context.Turmas.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
                    if (turma == null || !turma.Ativo)
                        return Results.NotFound(new { message = "Turma não existente" });

                    return Results.Ok(new
                    {
                        message = "Turma encontrada",
                        turma = new { turma.Id, turma.Nome, turma.CursoId, turma.ProfessorId, turma.Periodo }
                    });
                }).RequireAuthorization("Docencia");
        }
    }
}
