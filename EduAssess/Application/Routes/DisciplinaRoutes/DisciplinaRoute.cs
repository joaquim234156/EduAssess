using EduAssess.Application.DTOs.Disciplina;
using EduAssess.Application.Interfaces;
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
                async (DisciplinaRequest req, AppDbContext context, IDisciplinaService disciplinaService) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);
                    
                    var disciplina = await disciplinaService.CriarDisciplinaAsync(req);
                    return Results.Ok(new
                    {
                        message = "Disciplina registrada com sucesso",
                    });
                }).RequireAuthorization("Admin");

            route.MapGet("list", async (AppDbContext context, IDisciplinaService disciplinaService) =>
            {
                var disciplinas = await context.Disciplinas.ToListAsync();
                return Results.Ok(disciplinas.Select(d => new { d.Id, d.Nome, d.Descricao }));
            });

            route.MapPut("update/{id:Guid}",
            async ( Guid id, DisciplinaRequestPut req, AppDbContext context, IDisciplinaService disciplinaService)=>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req,out var errors))
                    return Results.BadRequest(errors);
                var disciplina = await context.Disciplinas.FindAsync(id);

                if (disciplina == null || disciplina.Ativa == false)
                {
                    return Results.NotFound(new {message = "Essa Disciplina não existe"});
                }
                
                await disciplinaService.AtualizarDisciplinaAsync(id, req);
                return Results.Ok(new
                {
                    message = "Disciplina atualizada com sucesso!!!"
                }
                );
            });

            route.MapDelete("delete/{id:Guid}",
            async (Guid id, AppDbContext context)=>
            {
                var disciplina = await context.Disciplinas.FindAsync(id);
                if(disciplina == null || !disciplina.Ativa)
                {
                    return Results.NotFound(new{message = "Disciplina não encontrada"});
                }
                disciplina.Disable();
                await context.SaveChangesAsync();
                return Results.Ok(new{message = "Disciplina deletada com sucesso"});
            }).RequireAuthorization("Admin");
        }
    }
}
