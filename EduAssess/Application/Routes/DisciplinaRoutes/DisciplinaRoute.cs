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
                    
                    var disciplina = new DisciplinaModel(req.nome, req.descricao, req.professorId);
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
            });
            route.MapPut("update/{id:Guid}",
            async ( Guid id, DisciplinaRequestPut req, AppDbContext context)=>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req,out var errors))
                    return Results.BadRequest(errors);
                var disciplina = await context.Disciplinas.FindAsync(id);

                if (disciplina == null || disciplina.Ativa == false)
                {
                    return Results.NotFound(new {message = "Essa Disciplina não existe"});
                }
                
                disciplina.UpdateDisciplina(req.nome, req.descricao,req.professorId);
                await context.SaveChangesAsync();
                return Results.Ok(new
                {
                    message = "Disciplina atualizada com sucesso!!!"
                }
                );
            });
            route.MapDelete("delete/{id:Guid}",
            async (Guid id, AppDbContext context )=>
            {
                var disciplina = await context.Disciplinas.FindAsync(id);
                if(disciplina == null || !disciplina.Ativa)
                {
                    return Results.NotFound(new{message = "Disciplina não encontrada"});
                }
                disciplina.Disable();
                await context.SaveChangesAsync();
                return Results.Ok(new{message = "Disciplina deletada com sucesso"});
            }).RequireAuthorization("Admin","Gestor");
        }
    }
}
