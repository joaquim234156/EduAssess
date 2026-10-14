using EduAssess.Application.DTOs.Matricula;
using EduAssess.Data;
using EduAssess.Models;

namespace EduAssess.Application.Routes
{
    public static class MatriculaRoute
    {
        public static void MatriculaRoutes(this WebApplication app)
        {
            var route = app.MapGroup("/matricula");

            route.MapPost("/create", async (MatriculaRequest req, AppDbContext context) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);
                var aluno = await context.Alunos.FindAsync(req.alunoId);
                if (aluno == null)
                    return Results.NotFound(new { message = "Aluno não encontrado" });
                var curso = await context.Cursos.FindAsync(req.cursoId);
                if (curso == null || !curso.Ativo)
                    return Results.NotFound(new { message = "Curso não encontrado" });
                var matricula = new MatriculaModel(aluno.Id, curso.Id);
                await context.AddAsync(matricula);
                await context.SaveChangesAsync();
                return Results.Ok(new { message = "Matrícula criada com sucesso", matriculaId = matricula.Id });
            });

            route.MapGet("/get/{id:guid}", async (Guid id, AppDbContext context) =>
            {
                var matricula = await context.Matriculas
                    .Include(m => m.Aluno)
                    .Include(m => m.Curso)
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (matricula == null)
                    return Results.NotFound(new { message = "Matrícula não encontrada" });
                return Results.Ok(new
                {
                    matricula.Id,
                    Aluno = new { matricula.Aluno.Id, matricula.Aluno.Nome },
                    Curso = new { matricula.Curso.Id, matricula.Curso.Nome },
                    matricula.DataMatricula
                });
            });

            route.MapDelete("/delete/{id:guid}", async (Guid id, AppDbContext context) =>
            {
                var matricula = await context.Matriculas.FindAsync(id);
                if (matricula == null)
                    return Results.NotFound(new { message = "Matrícula não encontrada" });
                context.Matriculas.Remove(matricula);
                await context.SaveChangesAsync();
                return Results.Ok(new { message = "Matrícula removida com sucesso" });
            });

            route.MapPut("/update/{id:guid}", async (Guid id, MatriculaRequest req, AppDbContext context) =>
            {
                var matricula = await context.Matriculas.FindAsync(id);
                if (matricula == null)
                    return Results.NotFound(new { message = "Matrícula não encontrada" });
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);
                matricula.AlunoId = req.alunoId;
                matricula.CursoId = req.cursoId;
                await context.SaveChangesAsync();
                return Results.Ok(new { message = "Matrícula atualizada com sucesso" });
            });
        }
    }
}
