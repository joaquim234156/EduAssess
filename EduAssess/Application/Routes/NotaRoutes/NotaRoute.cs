using System.Security.Claims;
using EduAssess.Application.DTOs.Nota;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Routes
{
    public static class NotaRoute
    {
        public static void NotaRoutes(this WebApplication app)
        {
            var route = app.MapGroup("notas").WithTags("Notas");

            route.MapPost("registrar", async (NotaRequest req, INotaService notaService, ClaimsPrincipal principal) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);

                // Professores só lançam notas em seu próprio nome.
                var currentUserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (principal.IsInRole("Professor") && currentUserId != req.idUsuario.ToString())
                    return Results.Forbid();

                try
                {
                    var resultado = await notaService.RegistrarNotaAsync(req);
                    return Results.Ok(resultado);
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            }).RequireAuthorization("Docencia");

            route.MapGet("listar", async (INotaService notaService) =>
            {
                var notas = await notaService.ListarTodasAsync();
                return Results.Ok(notas);
            }).RequireAuthorization("Gestao");

            route.MapGet("minhas", async (INotaService notaService, ClaimsPrincipal principal) =>
            {
                if (!Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                    return Results.Unauthorized();
                return Results.Ok(await notaService.ObterNotasDoAlunoAsync(userId));
            }).RequireAuthorization();

            route.MapGet("aluno/{idMatricula:guid}", async (Guid idMatricula, INotaService notaService, AppDbContext context, ClaimsPrincipal principal) =>
            {
                // Docência vê qualquer matrícula; o aluno só vê as notas da própria matrícula.
                if (!principal.IsInRole("Admin") && !principal.IsInRole("Gestor") && !principal.IsInRole("Professor"))
                {
                    var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var ownerId = await context.Matriculas.AsNoTracking()
                        .Where(m => m.Id == idMatricula).Select(m => m.UsuarioId).FirstOrDefaultAsync();
                    if (userId is null || ownerId.ToString() != userId)
                        return Results.Forbid();
                }

                var notasDoAluno = (await notaService.ObterNotasPorMatriculaAsync(idMatricula)).ToList();
                if (notasDoAluno.Count == 0)
                    return Results.NotFound(new { mensagem = "Nenhuma nota encontrada para esta matrícula." });

                return Results.Ok(notasDoAluno);
            }).RequireAuthorization();
        }
    }
}
