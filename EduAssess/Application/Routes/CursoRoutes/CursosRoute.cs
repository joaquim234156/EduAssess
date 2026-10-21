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

            route.MapDelete("delete/{id:Guid}",
                async (Guid id, ICursoService cursoService) =>
                {
                    var deleted = await cursoService.DeletarCursoAsync(id);
                    if (!deleted)
                    {
                        return Results.NotFound(new { message = "Curso não encontrada" });
                    }
                    return Results.Ok(new { message = "Curso deletado com sucesso" });
                }).RequireAuthorization("Admin");

            route.MapGet("get/{id:guid}",
                async (Guid id, ICursoService CursoService) =>
                {
                    var curso = await CursoService.ObterCursoPorIdAsync(id);
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
                    var cursos = await CursoService.ListarCursosAsync();
                    return Results.Ok(new { message = "Cursos listados com sucesso", cursos });
                }).RequireAuthorization("Admin");

            route.MapGet("list/ativos",
                async (AppDbContext context, ICursoService CursoService) =>
                {
                    var cursos = await CursoService.ListarCursosAtivosAsync();
                    return Results.Ok(new { message = "Cursos ativos listados com sucesso", cursos });
                }).RequireAuthorization("Admin");

            route.MapGet("list/inativos",
                async (AppDbContext context, ICursoService CursoService) =>
                {
                    var cursos = await CursoService.ListarCursosInativosAsync();
                    return Results.Ok(new { message = "Cursos inativos listados com sucesso", cursos });
                }).RequireAuthorization("Admin");

            route.MapPut("ativar/{id:guid}",
                async (Guid id, ICursoService CursoService) =>
                {
                    var enable = await CursoService.AtivarCursoAsync(id);
                    if (!enable)
                        return Results.NotFound(new { message = "Curso não encontrado ou já ativo" });
                    return Results.Ok(new { message = "Curso ativado com sucesso" });
                }).RequireAuthorization("Admin");

        }

    }
}
