using EduAssess.Application.DTOs.Curso;
using EduAssess.Application.Interfaces;
using EduAssess.Application.Services;
using EduAssess.Data;
using EduAssess.Models;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Routes
{
    public static class CursosRoute
    {
        public static void CursosRoutes(this WebApplication app)
        {
            var route = app.MapGroup("cursos").WithTags("Cursos");

            route.MapPost("create",
                async (CursoRequest req, AppDbContext context, ICursoService CursoService) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    var existingCurso = await context.Cursos.FirstOrDefaultAsync(c => c.Nome == req.nome);
                    if (existingCurso != null)
                        return Results.BadRequest(new { message = "Curso já existente" });

                    var curso = await CursoService.CriarCursoAsync(req);

                    return Results.Ok(new { message = "Curso criado com sucesso", cursoId = curso.Id });
                }).RequireAuthorization("Admin");

            route.MapPut("update/{id:guid}",
            async (Guid id, CursoRequest req, AppDbContext context, ICursoService CursoService) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);

                var curso = await context.Cursos.FindAsync(id);
                if (curso == null || !curso.Ativo)
                    return Results.NotFound(new { message = "Curso não encontrado" });

                await CursoService.AtualizarCursoAsync(id, req);
                return Results.Ok(new
                {
                    message = "Curso atualizado com sucesso",
                    curso = new
                    {
                        curso.Id,
                        curso.Nome,
                        curso.Descricao,
                        curso.DuracaoEmAnos
                    }
                });
            }).RequireAuthorization("Admin");

            route.MapDelete("delete/{id:guid}",
            async (Guid id, AppDbContext context, ICursoService CursoService) =>
            {
                var curso = await context.Cursos.FindAsync(id);
                if (curso == null || !curso.Ativo)
                    return Results.NotFound(new { message = "Curso não encontrado" });

                curso.Disable();
                await context.SaveChangesAsync();
                return Results.Ok(new { message = "Curso desativado com sucesso" });
            }).RequireAuthorization("Admin");

            route.MapGet("get/{id:guid}",
                async (Guid id, AppDbContext context, ICursoService CursoService) =>
                {
                    var curso = await context.Cursos.FindAsync(id);
                    if (curso == null || !curso.Ativo)
                        return Results.Ok(new { message = "Curso não encontrado" });
                    return Results.Ok(new
                    {
                        message = "curso encontrado com sucesso",
                        curso = new
                        {
                            curso.Id,
                            curso.Nome,
                            curso.Descricao,
                            curso.DuracaoEmAnos
                        }
                    });
                }).RequireAuthorization("Admin");

            route.MapGet("list",
                async (AppDbContext context, ICursoService CursoService) =>
                {
                    var cursos = await CursoService.ListarTodosAsync();
                    return Results.Ok(new { message = "Cursos listados com sucesso", cursos });
                }).RequireAuthorization("Admin");
        }

    }
}
