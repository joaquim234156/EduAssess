using EduAssess.Application.DTOs.Nota;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Routes
{
    public static class NotaRoute
    {
        public static void NotaRoutes(this WebApplication app)
        {
            var route = app.MapGroup("notas").WithTags("Notas");

            route.MapPost("registrar", async (NotaRequest req, INotaService notaService) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);  

                var resultado = await notaService.RegistrarNotaAsync(req);
                return Results.Ok(resultado);
            }).RequireAuthorization("Professor");

            route.MapGet("listar", async (INotaService notaService) =>
            {
                var notas = await notaService.ListarTodasAsync();
                return Results.Ok(notas);
            }).RequireAuthorization("Professor");

            route.MapGet("aluno/{idMatricula:guid}", async (Guid idMatricula, INotaService notaService) =>
            {
                var notasDoAluno = await notaService.ObterNotasPorMatriculaAsync(idMatricula);

                if (!notasDoAluno.Any())
                    return Results.NotFound(new { mensagem = "Nenhuma nota encontrada para este aluno." });

                return Results.Ok(notasDoAluno);
            }).RequireAuthorization("Professor");
        }
    }
}
