using EduAssess.Application.DTOs.Turma;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Routes
{
    public static class TurmaRoute
    {
        public static void TurmaRoutes(this WebApplication app)
        {
            var route = app.MapGroup("turmas").WithTags("Turmas");

            route.MapPost("create",
                async (TurmaRequest req, AppDbContext context) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    if (!await context.Cursos.AnyAsync(c => c.Id == req.cursoId && c.Ativo))
                        return Results.BadRequest(new { message = "Selecione um curso ativo." });
                    if (!await context.Users.AnyAsync(u => u.Id == req.professorId && u.Ativa && u.Role == UserRole.Professor))
                        return Results.BadRequest(new { message = "Selecione um professor ativo." });

                    var nome = req.nome.Trim();
                    if (await context.Turmas.AnyAsync(t => t.Ativa && t.Nome == nome))
                        return Results.BadRequest(new { message = "Turma já existente" });

                    var turma = new TurmaModel(nome, req.professorId, req.cursoId);
                    await context.Turmas.AddAsync(turma);
                    await context.SaveChangesAsync();
                    return Results.Ok(new { message = "Turma criada com sucesso", turmaId = turma.Id });
                }).RequireAuthorization("Gestao");

            route.MapGet("list",
                async (AppDbContext context) =>
                {
                    var turmas = await context.Turmas.AsNoTracking()
                        .Where(t => t.Ativa)
                        .OrderBy(t => t.Nome)
                        .Select(t => new TurmaResponseDTO(t.Id, t.Nome, t.CursoId, t.ProfessorId, t.Periodo, t.Ativa))
                        .ToListAsync();
                    return Results.Ok(turmas);
                }).RequireAuthorization("Gestao");

            route.MapGet("list/professor/{professorId:guid}",
                async (Guid professorId, ITurmaService turmaService) =>
                {
                    var turmas = await turmaService.ListarTurmasPorProfessorAsync(professorId);
                    return Results.Ok(new { message = "Lista de turmas do professor", turmas });
                }).RequireAuthorization("Admin");


            route.MapGet("get/{id:guid}",
                async (Guid id, AppDbContext context) =>
                {
                    var turma = await context.Turmas.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
                    if (turma == null || !turma.Ativa)
                        return Results.NotFound(new { message = "Turma não existente" });

                    return Results.Ok(new
                    {
                        message = "Turma encontrada",
                        turma = new { turma.Id, turma.Nome, turma.CursoId, turma.ProfessorId, turma.Periodo }
                    });
                }).RequireAuthorization("Admin");

            route.MapPut("update/{id:guid}",
                async (Guid id, TurmaRequestPut req, AppDbContext context, ITurmaService turmaService) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    if (req.professorId == Guid.Empty || req.cursoId == Guid.Empty)
                        return Results.BadRequest(new { message = "Informe IDs válidos para professor e curso." });

                    // Mantém o vínculo apenas com um professor existente, ativo e com a role Professor.
                    var professorValido = await context.Users.AnyAsync(user =>
                        user.Id == req.professorId && user.Ativa && user.Role == UserRole.Professor);
                    if (!professorValido)
                        return Results.BadRequest(new { message = "O ID informado não pertence a um professor ativo." });

                    // Impede trocar a turma para um curso inexistente ou desativado.
                    var cursoAtivo = await context.Cursos.AnyAsync(curso =>
                        curso.Id == req.cursoId && curso.Ativo);
                    if (!cursoAtivo)
                        return Results.BadRequest(new { message = "O curso informado não existe ou está inativo." });

                    var existingTurma = await context.Turmas.FirstOrDefaultAsync(t => t.Id == id);
                    if (existingTurma == null)
                        return Results.NotFound(new { message = "Turma não existente" });
                    var updatedTurma = await turmaService.AtualizarTurmaAsync(id, req);
                    return Results.Ok(new { message = "Turma atualizada com sucesso", turmaId = updatedTurma.IdTurma });
                }).RequireAuthorization("Admin");

            route.MapDelete("delete/{id:guid}",
                async (Guid id, AppDbContext context, ITurmaService turmaService) =>
                {
                    var existingTurma = await context.Turmas.FirstOrDefaultAsync(t => t.Id == id);
                    if (existingTurma == null)
                        return Results.NotFound(new { message = "Turma não existente" });
                    var deleted = await turmaService.DeletarTurmaAsync(id);
                    if (!deleted)
                        return Results.BadRequest(new { message = "Erro ao deletar a turma" });
                    return Results.Ok(new { message = "Turma deletada com sucesso" });
                }).RequireAuthorization("Admin");

            route.MapPut("activate/{id:guid}",
                async (Guid id, AppDbContext context, ITurmaService turmaService) =>
                {
                    var existingTurma = await context.Turmas.FirstOrDefaultAsync(t => t.Id == id);
                    if (existingTurma == null)
                        return Results.NotFound(new { message = "Turma não existente" });
                    var activated = await turmaService.AtivarTurmaAsync(id);
                    if (!activated)
                        return Results.BadRequest(new { message = "Erro ao ativar a turma" });
                    return Results.Ok(new { message = "Turma ativada com sucesso" });
                }).RequireAuthorization("Admin");
        }
    }
}
