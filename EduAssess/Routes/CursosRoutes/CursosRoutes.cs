using EduAssess.Data;
using EduAssess.Models.CursoModel;
using EduAssess.Services;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Routes.CursosRoutes
{
    public static class CursosRoutes
    {
        public static void MapCursosRoutes(this WebApplication app)
        {
            var route = app.MapGroup("cursos");
            route.MapPost("create",
                async (CursosRequest req, AppDbContext context) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    var existingCurso = await context.Cursos.FirstOrDefaultAsync(c => c.Nome == req.nome);
                    if (existingCurso != null)
                        return Results.BadRequest(new { message = "Curso já existente" });

                    var curso = new CursoModel(req.nome, req.descricao, req.professor, req.duracaoEmAnos);
                    await context.Cursos.AddAsync(curso);
                    await context.SaveChangesAsync();

                    return Results.Ok(new { message = "Curso criado com sucesso", cursoId = curso.Id });
                });
            route.MapPut("update/{id:guid}",
            async (Guid id, CursosRequest req, AppDbContext context) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);

                var curso = await context.Cursos.FindAsync(id);
                if (curso == null || !curso.Ativo)
                    return Results.NotFound(new { message = "Curso não encontrado" });

                curso.Update(req.nome, req.descricao, req.professor, req.duracaoEmAnos);
                await context.SaveChangesAsync();
                return Results.Ok(new
                {
                    message = "Curso atualizado com sucesso",
                    curso = new
                    {
                        curso.Id,
                        curso.Nome,
                        curso.Descricao,
                        curso.Professor,
                        curso.DuracaoEmAnos
                    }
                });
            });
            route.MapPut("delete/{id:guid}",
            async (Guid id, CursosRequest req, AppDbContext context) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);

                var curso = await context.Cursos.FindAsync(id);
                if (curso == null || !curso.Ativo)
                    return Results.NotFound(new { message = "Curso não encontrado" });

                curso.Disable();
                await context.SaveChangesAsync(); return Results.Ok(new
                {
                    message = "Curso desativado com sucesso",
                    curso = new
                    {
                        curso.Id,
                        curso.Nome,
                        curso.Descricao,
                        curso.Professor,
                        curso.DuracaoEmAnos
                    }

                });
            });
            route.MapGet("get/{nome}",
                async (string nome, AppDbContext context) =>
                {
                    var curso = await context.Cursos.FirstOrDefaultAsync(c => c.Nome == nome);
                    if (curso == null || !curso.Ativo)
                        return Results.Ok(new { message = "Curso não encontrado" });
                    return Results.Ok(new {
                        message = "curso encontrado com sucesso",
                        curso = new
                        {
                            curso.Id,
                            curso.Nome,
                            curso.Descricao,
                            curso.Professor,
                            curso.DuracaoEmAnos
                        }
                    });
                });
        }

    }
}
