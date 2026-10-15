using EduAssess.Application.DTOs.Matricula;
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
                async(MatriculaRequest req, AppDbContext context, IConfiguration config) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    var matricula = new MatriculaModel(req.usuarioId, req.turmaId);

                    context.Matriculas.Add(matricula);
                    await context.SaveChangesAsync();

                    return Results.Ok(new
                    {
                        message = "Matricula registrada com sucesso",
                        matricula = new {matricula.Id, matricula.UsuarioId, matricula.TurmaId, matricula.DataMatricula}
                    });
                }).RequireAuthorization("Gestao", "Admin");

            route.MapGet("matriculas",
                async (AppDbContext context) =>
                {
                    var matriculas = await context.Matriculas.Select(m => new MatriculaResponseDTO(m.Id, m.UsuarioId, m.TurmaId, m.DataMatricula)).ToListAsync();
                    return Results.Ok(matriculas);
                }).RequireAuthorization("Gestao", "Admin");
        }
    }
}