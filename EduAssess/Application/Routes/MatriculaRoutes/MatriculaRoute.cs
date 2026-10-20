using EduAssess.Application.DTOs.Matricula;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Routes
{
    public static class MatriculaRoute
    {
        public static void MatriculaRoutes(this WebApplication app)
        {
            var route = app.MapGroup("/matricula").WithTags("Matriculas");

            route.MapPost("register",
                async(MatriculaRequest req, IMatriculaService matriculaService, IConfiguration config) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    var matricula = await matriculaService.CriarMatriculaAsync(req);
                    return Results.Ok(new
                    {
                        message = "Matricula registrada com sucesso",
                        matricula = new {matricula.Id, matricula.UsuarioId, matricula.TurmaId, matricula.DataMatricula}
                    });
                }).RequireAuthorization("Admin");

            route.MapGet("matriculas",
                async (IMatriculaService matriculaService) =>
                {
                    var matriculas = await matriculaService.ListarMatriculasAsync();
                    return Results.Ok(matriculas);
                }).RequireAuthorization("Admin");

            route.MapGet("matricula/{id:Guid}",
                async (Guid id, IMatriculaService matriculaService) =>
                {
                    var matricula = await matriculaService.ObterMatriculaPorIdAsync(id);
                    if (matricula == null)
                    {
                        return Results.NotFound(new { message = "Matrícula não encontrada" });
                    }
                    return Results.Ok(matricula);
                }).RequireAuthorization("Admin");

            route.MapPut("matricula/{id:Guid}",
                async (Guid id, MatriculaRequestPut req, IMatriculaService matriculaService) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);
                    var updatedMatricula = await matriculaService.AtualizarMatriculaAsync(id, req);
                    if (updatedMatricula == null)
                    {
                        return Results.NotFound(new { message = "Matrícula não encontrada" });
                    }
                    return Results.Ok(new
                    {
                        message = "Matrícula atualizada com sucesso",
                        matricula = new { updatedMatricula.Id, updatedMatricula.UsuarioId, updatedMatricula.TurmaId, updatedMatricula.DataMatricula }
                    });
                }).RequireAuthorization("Admin");

            route.MapDelete("matricula/{id:Guid}",
                async (Guid id, IMatriculaService matriculaService) =>
                {
                    var deleted = await matriculaService.DeletarMatriculaAsync(id);
                    if (!deleted)
                    {
                        return Results.NotFound(new { message = "Matrícula não encontrada" });
                    }
                    return Results.Ok(new { message = "Matrícula deletada com sucesso" });
                }).RequireAuthorization("Admin");
        }
    }
}