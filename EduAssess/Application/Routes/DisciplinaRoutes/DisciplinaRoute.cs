using EduAssess.Application.DTOs.Disciplina;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduAssess.Application.Routes
{
    public static class DisciplinaRoute
    {
        public static void DisciplinaRoutes(this WebApplication app)
        {
            var route = app.MapGroup("/disciplina").WithTags("Disciplinas");

            route.MapPost("register",
                async (DisciplinaRequest req, ClaimsPrincipal user, AppDbContext context, IDisciplinaService disciplinaService) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    // Professor usa sua identidade do JWT; Admin/Gestor escolhem o professor responsável.
                    Guid professorId;
                    if (user.IsInRole("Professor"))
                    {
                        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (!Guid.TryParse(userId, out professorId))
                            return Results.Unauthorized();

                        if (req.professorId.HasValue && req.professorId.Value != professorId)
                            return Results.Forbid();
                    }
                    else
                    {
                        if (!req.professorId.HasValue)
                            return Results.BadRequest(new { message = "O Admin deve informar o ID do professor." });

                        professorId = req.professorId.Value;
                    }

                    // Impede vincular disciplina a conta inexistente, inativa ou sem o perfil Professor.
                    var professorValido = await context.Users.AnyAsync(u =>
                        u.Id == professorId && u.Ativa && u.Role == UserRole.Professor);

                    if (!professorValido)
                        return Results.BadRequest(new { message = "O ID informado não pertence a um professor ativo." });

                    await disciplinaService.CriarDisciplinaAsync(req, professorId);
                    return Results.Ok(new
                    {
                        message = "Disciplina registrada com sucesso",
                    });
                // Mantém Admin/Gestor e acrescenta Professor ao fluxo de criação.
                }).RequireAuthorization(policy => policy.RequireRole("Admin", "Gestor", "Professor"));

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

                // Rejeita o GUID vazio antes de tentar persistir uma referência de professor inválida.
                if (req.professorId == Guid.Empty)
                    return Results.BadRequest(new { message = "Informe o ID de um professor válido." });

                // A edição só pode atribuir a disciplina a um professor ativo, não a um ID qualquer.
                var professorValido = await context.Users.AnyAsync(u =>
                    u.Id == req.professorId && u.Ativa && u.Role == UserRole.Professor);
                if (!professorValido)
                    return Results.BadRequest(new { message = "O ID informado não pertence a um professor ativo." });

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
            // A policy Admin inclui Admin e Gestor, mantendo o acesso administrativo da rota.
            }).RequireAuthorization("Admin");

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
