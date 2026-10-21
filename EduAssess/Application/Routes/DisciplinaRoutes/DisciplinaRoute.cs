using EduAssess.Application.DTOs.Disciplina;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models.Local;
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

                    var professorValido = await context.Users.AnyAsync(u =>
                        u.Id == professorId && u.Ativa && u.Role == UserRole.Professor);

                    if (!professorValido)
                        return Results.BadRequest(new { message = "O ID informado não pertence a um professor ativo." });

                    await disciplinaService.CriarDisciplinaAsync(req, professorId);
                    return Results.Ok(new
                    {
                        message = "Disciplina registrada com sucesso",
                    });
                }).RequireAuthorization(policy => policy.RequireRole("Admin", "Gestor", "Professor"));

            route.MapGet("list", async (IDisciplinaService disciplinaService) =>
            {
                var disciplinas = await disciplinaService.ListarDisciplinasAsync();
                return Results.Ok(new { message = "Disciplinas listados com sucesso", disciplinas });
            }).RequireAuthorization("Admin");

            route.MapGet("list/ativas", async (IDisciplinaService disciplinaService) =>
            {
                var disciplinas = await disciplinaService.ListarDisciplinasAtivasAsync();
                return Results.Ok(new { message = "Disciplinas ativas listadas com sucesso", disciplinas });
            }).RequireAuthorization("Admin");

            route.MapGet("list/inativas", async (IDisciplinaService disciplinaService) =>
            {
                var disciplinas = await disciplinaService.ListarDisciplinasInativasAsync();
                return Results.Ok(new { message = "Disciplinas inativas listadas com sucesso", disciplinas });
            }).RequireAuthorization("Admin");

            route.MapGet("list/professor/{professorId:Guid}", async (Guid professorId, IDisciplinaService disciplinaService) =>
            {
                var disciplinas = await disciplinaService.ListarDisciplinasPorProfessorAsync(professorId);
                return Results.Ok(new { message = "Disciplinas do professor listadas com sucesso", disciplinas });
            }).RequireAuthorization("Admin");

            route.MapPut("update/{id:Guid}",
            async (Guid id, DisciplinaRequestPut req, AppDbContext context, IDisciplinaService disciplinaService) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);

                if (req.professorId == Guid.Empty)
                    return Results.BadRequest(new { message = "Informe o ID de um professor válido." });

                var professorValido = await context.Users.AnyAsync(u =>
                    u.Id == req.professorId && u.Ativa && u.Role == UserRole.Professor);
                if (!professorValido)
                    return Results.BadRequest(new { message = "O ID informado não pertence a um professor ativo." });

                var disciplina = await context.Disciplinas.FindAsync(id);

                if (disciplina == null || disciplina.Ativa == false)
                {
                    return Results.NotFound(new { message = "Essa Disciplina não existe" });
                }

                await disciplinaService.AtualizarDisciplinaAsync(id, req);
                return Results.Ok(new
                {
                    message = "Disciplina atualizada com sucesso!!!"
                }
                );
            }).RequireAuthorization("Admin");

            route.MapDelete("delete/{id:Guid}",
            async (Guid id, IDisciplinaService disciplinaService) =>
            {
                var deleted = await disciplinaService.DeletarDisciplinaAsync(id);
                if (!deleted)
                {
                    return Results.NotFound(new { message = "Disciplina não encontrada" });
                }
                return Results.Ok(new { message = "Disciplina deletada com sucesso" });
            }).RequireAuthorization("Admin");

            route.MapPut("ativar/{id:guid}",
                async (Guid id, IDisciplinaService disciplinaService) =>
                {
                    var enable = await disciplinaService.AtivarDisciplinaAsync(id);
                    if (!enable)
                        return Results.NotFound(new { message = "Disciplina não encontrada ou já ativa" });
                    return Results.Ok(new { message = "Disciplina ativada com sucesso" });
                }).RequireAuthorization("Admin");
        }
    }
}
