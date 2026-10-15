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
                    
                    var disciplina = new DisciplinaModel(req.nome, req.descricao);
                    context.Disciplinas.Add(disciplina);
                    await context.SaveChangesAsync();
                    return Results.Ok(new
                    {
                        message = "Disciplina registrada com sucesso",
                        disciplina = new { disciplina.Id, disciplina.Nome, disciplina.Descricao }
                    });
                }).RequireAuthorization("Admin", "Gestor");

            route.MapGet("list", async (AppDbContext context) =>
            {
                var disciplinas = await context.Disciplinas.ToListAsync();
                return Results.Ok(disciplinas.Select(d => new { d.Id, d.Nome, d.Descricao }));
            }).RequireAuthorization("Admin", "Gestor", "Professor", "Aluno");
        }
    }
}
