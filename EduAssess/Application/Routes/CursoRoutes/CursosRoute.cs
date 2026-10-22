using EduAssess.Application.DTOs.Curso;
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

                    var nome = req.nome.Trim();
                    if (await context.Cursos.AnyAsync(c => c.Ativo && c.Nome == nome))
                        return Results.BadRequest(new { message = "Curso já existente" });

                    var curso = new CursoModel(nome, (req.descricao ?? "").Trim(), req.duracaoEmAnos);
                    context.Cursos.Add(curso);
                    await context.SaveChangesAsync();

                    return Results.Ok(new { message = "Curso criado com sucesso", cursoId = curso.Id });
                }).RequireAuthorization("Admin");

            route.MapGet("list",
                async (AppDbContext context) =>
                {
                    var cursos = await context.Cursos.AsNoTracking()
                        .Where(c => c.Ativo)
                        .OrderBy(c => c.Nome)
                        .Select(c => new CursoResponseDTO(c.Id, c.Nome, c.Descricao, c.DuracaoEmAnos, c.Ativo))
                        .ToListAsync();
                    return Results.Ok(cursos);
                }).RequireAuthorization("Gestao");

            route.MapPut("update/{id:guid}",
                async (Guid id, CursosRequest req, AppDbContext context) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    var curso = await context.Cursos.FindAsync(id);
                    if (curso == null || !curso.Ativo)
                        return Results.NotFound(new { message = "Curso não encontrado" });

                    var nome = req.nome.Trim();
                    if (await context.Cursos.AnyAsync(c => c.Ativo && c.Nome == nome && c.Id != id))
                        return Results.BadRequest(new { message = "Já existe um curso ativo com esse nome" });

                    curso.Update(nome, (req.descricao ?? "").Trim(), req.duracaoEmAnos);
                    await context.SaveChangesAsync();
                    return Results.Ok(new
                    {
                        message = "Curso atualizado com sucesso",
                        curso = new { curso.Id, curso.Nome, curso.Descricao, curso.DuracaoEmAnos }
                    });
                }).RequireAuthorization("Gestao");

            route.MapDelete("delete/{id:guid}",
                async (Guid id, AppDbContext context) =>
                {
                    var curso = await context.Cursos.FindAsync(id);
                    if (curso == null || !curso.Ativo)
                        return Results.NotFound(new { message = "Curso não encontrado" });

                    curso.Disable();
                    await context.SaveChangesAsync();
                    return Results.Ok(new { message = "Curso desativado com sucesso" });
                }).RequireAuthorization("Gestao");

            route.MapGet("get/{nome}",
                async (string nome, AppDbContext context) =>
                {
                    var curso = await context.Cursos.AsNoTracking().FirstOrDefaultAsync(c => c.Ativo && c.Nome == nome);
                    if (curso == null)
                        return Results.NotFound(new { message = "Curso não encontrado" });

                    return Results.Ok(new
                    {
                        message = "Curso encontrado com sucesso",
                        curso = new { curso.Id, curso.Nome, curso.Descricao, curso.DuracaoEmAnos }
                    });
                }).RequireAuthorization("Admin");

            route.MapGet("list",
                async (ICursoService CursoService) =>
                {
                    var cursos = await CursoService.ListarCursosAsync();
                    return Results.Ok(new { message = "Cursos listados com sucesso", cursos });
                }).RequireAuthorization("Admin");

            route.MapGet("list/ativos",
                async (ICursoService CursoService) =>
                {
                    var cursos = await CursoService.ListarCursosAtivosAsync();
                    return Results.Ok(new { message = "Cursos ativos listados com sucesso", cursos });
                }).RequireAuthorization("Admin");

            route.MapGet("list/inativos",
                async (ICursoService CursoService) =>
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
