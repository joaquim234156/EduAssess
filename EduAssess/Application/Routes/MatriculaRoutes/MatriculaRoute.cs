using EduAssess.Application.DTOs.Matricula;
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
                async (MatriculaRequest req, AppDbContext context) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    if (!await context.Users.AnyAsync(u => u.Id == req.usuarioId && u.Ativa && u.Role == UserRole.Aluno))
                        return Results.BadRequest(new { message = "Selecione um aluno ativo." });
                    if (!await context.Turmas.AnyAsync(t => t.Id == req.turmaId && t.Ativa))
                        return Results.BadRequest(new { message = "Selecione uma turma ativa." });
                    if (await context.Matriculas.AnyAsync(m => m.UsuarioId == req.usuarioId && m.TurmaId == req.turmaId && m.Ativa))
                        return Results.Conflict(new { message = "Este aluno já está matriculado nesta turma." });

                    var matricula = new MatriculaModel(req.usuarioId, req.turmaId);
                    context.Matriculas.Add(matricula);
                    await context.SaveChangesAsync();

                    return Results.Ok(new
                    {
                        message = "Matrícula registrada com sucesso",
                        matricula = new { matricula.Id, matricula.UsuarioId, matricula.TurmaId, matricula.DataMatricula }
                    });
                }).RequireAuthorization("Gestao");

            route.MapGet("matriculas",
                async (AppDbContext context) =>
                {
                    var matriculas = await context.Matriculas.AsNoTracking()
                        .OrderByDescending(m => m.DataMatricula)
                        .Select(m => new MatriculaResponseDTO(m.Id, m.UsuarioId, m.TurmaId, m.DataMatricula, m.Ativa))
                        .ToListAsync();
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
        }
    }
}
