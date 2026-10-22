using EduAssess.Application.DTOs.Disciplina;
using EduAssess.Data;
using EduAssess.Models;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Routes
{
    public static class DisciplinaRoute
    {
        public static void DisciplinaRoutes(this WebApplication app)
        {
            var route = app.MapGroup("/disciplina").WithTags("Disciplinas");

            route.MapPost("register",
                async (DisciplinaRequest req, AppDbContext context) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    if (!await context.Users.AnyAsync(u => u.Id == req.professorId && u.Ativa && u.Role == UserRole.Professor))
                        return Results.BadRequest(new { message = "Selecione um professor ativo." });

                    var nome = req.nome.Trim();
                    if (await context.Disciplinas.AnyAsync(d => d.Ativa && d.Nome == nome))
                        return Results.BadRequest(new { message = "Já existe uma disciplina ativa com esse nome." });

                    var disciplina = new DisciplinaModel(nome, (req.descricao ?? "").Trim(), req.professorId);
                    context.Disciplinas.Add(disciplina);
                    await context.SaveChangesAsync();
                    return Results.Ok(new
                    {
                        message = "Disciplina registrada com sucesso",
                        disciplina = new { disciplina.Id, disciplina.Nome, disciplina.Descricao, disciplina.ProfessorId }
                    });
                }).RequireAuthorization("Gestao");

            route.MapGet("list", async (AppDbContext context) =>
            {
                var disciplinas = await context.Disciplinas.AsNoTracking()
                    .Where(d => d.Ativa)
                    .OrderBy(d => d.Nome)
                    .Select(d => new DisciplinaResponseDTO(d.Id, d.Nome, d.Descricao, d.ProfessorId))
                    .ToListAsync();
                return Results.Ok(disciplinas);
            }).RequireAuthorization();

            route.MapPut("update/{id:guid}",
                async (Guid id, DisciplinaRequestPut req, AppDbContext context) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    var disciplina = await context.Disciplinas.FindAsync(id);
                    if (disciplina == null || !disciplina.Ativa)
                        return Results.NotFound(new { message = "Essa disciplina não existe" });

                    if (!await context.Users.AnyAsync(u => u.Id == req.professorId && u.Ativa && u.Role == UserRole.Professor))
                        return Results.BadRequest(new { message = "Selecione um professor ativo." });

                    disciplina.UpdateDisciplina(req.nome.Trim(), (req.descricao ?? "").Trim(), req.professorId);
                    await context.SaveChangesAsync();
                    return Results.Ok(new { message = "Disciplina atualizada com sucesso" });
                }).RequireAuthorization("Gestao");

            route.MapDelete("delete/{id:guid}",
                async (Guid id, AppDbContext context) =>
                {
                    var disciplina = await context.Disciplinas.FindAsync(id);
                    if (disciplina == null || !disciplina.Ativa)
                        return Results.NotFound(new { message = "Disciplina não encontrada" });

                    disciplina.Disable();
                    await context.SaveChangesAsync();
                    return Results.Ok(new { message = "Disciplina desativada com sucesso" });
                }).RequireAuthorization("Gestao");
        }
    }
}
