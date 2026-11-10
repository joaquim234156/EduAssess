using EduAssess.Application.DTOs.Matricula;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Routes
{
    public static class MatriculaRoute
    {
        public static void MatriculaRoutes(this WebApplication app)
        {
            var route = app.MapGroup("/matricula").WithTags("Matriculas");

            route.MapPost("register",
                async (MatriculaRequest req, AppDbContext context, IMatriculaService matriculaService) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    if (!await context.Users.AnyAsync(u => u.Id == req.usuarioId && u.Ativa && u.Role == UserRole.Aluno))
                        return Results.BadRequest(new { message = "Selecione um aluno ativo." });
                    if (!await context.Turmas.AnyAsync(t => t.Id == req.turmaId && t.Ativa))
                        return Results.BadRequest(new { message = "Selecione uma turma ativa." });
                    if (await context.Matriculas.AnyAsync(m => m.UsuarioId == req.usuarioId && m.TurmaId == req.turmaId && m.Ativa))
                        return Results.Conflict(new { message = "Este aluno já está matriculado nesta turma." });

                    var matricula = await matriculaService.CriarMatriculaAsync(req);

                    return Results.Ok(new
                    {
                        message = "Matrícula registrada com sucesso",
                        matricula = new { matricula.Id, matricula.UsuarioId, matricula.TurmaId, matricula.DataMatricula }
                    });
                }).RequireAuthorization("Admin");

            route.MapGet("matriculas",
            async (IMatriculaService matriculaService) =>
            {
                var matriculas = await matriculaService.ListarMatriculasAsync();
                return Results.Ok(matriculas);
            }).RequireAuthorization("Gestao");


            route.MapDelete("end/{id:guid}",
                async (Guid id, AppDbContext context) =>
                {
                    var matricula = await context.Matriculas.FindAsync(id);
                    if (matricula == null || !matricula.Ativa)
                        return Results.NotFound(new { message = "Matrícula não encontrada" });

                    matricula.Disable();
                    await context.SaveChangesAsync();
                    return Results.Ok(new { message = "Matrícula encerrada com sucesso" });
                }).RequireAuthorization("Gestao");

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
