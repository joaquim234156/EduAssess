using System.Security.Claims;
using EduAssess.Application.DTOs.UsuarioInstituicao;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models.Global;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Routes.UsuarioInstituicaoRoutes;

public static class UsuarioInstituicaoRoute
{
    /// <summary>
    /// Operações de vínculo são sempre limitadas à instituição alvo. Compartilhar mantenedor não
    /// permite que o Gestor de uma instituição administre usuários de outra instituição da rede.
    /// </summary>
    public static void UsuarioInstituicaoRoutes(this WebApplication app)
    {
        var route = app.MapGroup("/usuario-instituicao").WithTags("UsuarioInstituicao");

        route.MapPost("vincular", async (UsuarioInstituicaoRequest req, IUsuarioInstituicaoService service,
            AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!MiniValidation.MiniValidator.TryValidate(req, out var errors)) return Results.BadRequest(errors);
            if (!await CanManageInstitutionAsync(db, user, req.InstituicaoId)) return Results.Forbid();

            // Compartilhamento de contas entre instituições exige Admin. O mantenedor não amplia o
            // escopo do Gestor, inclusive quando a tentativa vem diretamente pela API.
            if (!user.IsInRole(nameof(UserRole.Admin)))
            {
                var alreadyLinkedHere = await db.UsuariosInstituicoes.AnyAsync(ui =>
                    ui.UsuarioId == req.UsuarioId && ui.InstituicaoId == req.InstituicaoId && ui.Ativa);
                var linkedElsewhere = await db.UsuariosInstituicoes.AnyAsync(ui =>
                    ui.UsuarioId == req.UsuarioId && ui.InstituicaoId != req.InstituicaoId && ui.Ativa);
                if (!alreadyLinkedHere && linkedElsewhere) return Results.Forbid();
            }

            try
            {
                var result = await service.VincularUsuarioInstituicaoAsync(req);
                return Results.Ok(new { message = "Usuário vinculado à instituição com sucesso", usuarioInstituicao = result });
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { message = ex.Message }); }
        }).RequireAuthorization("Gestao");

        route.MapPut("atualizar", async (UsuarioInstituicaoRequestPut req, IUsuarioInstituicaoService service,
            AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!MiniValidation.MiniValidator.TryValidate(req, out var errors)) return Results.BadRequest(errors);
            if (!await CanManageInstitutionAsync(db, user, req.InstituicaoId)) return Results.Forbid();
            try
            {
                var result = await service.AtualizarUsuarioInstituicaoAsync(req);
                return Results.Ok(new { message = "Vínculo atualizado com sucesso", usuarioInstituicao = result });
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).RequireAuthorization("Gestao");

        route.MapGet("verificar-vinculo", async (Guid usuarioId, Guid instituicaoId,
            IUsuarioInstituicaoService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!await CanManageInstitutionAsync(db, user, instituicaoId)) return Results.NotFound();
            var linked = await service.VerificarVinculoUsuarioInstituicaoAsync(usuarioId, instituicaoId);
            return Results.Ok(new { vinculado = linked });
        }).RequireAuthorization("Gestao");

        route.MapDelete("desvincular", async (Guid usuarioId, Guid instituicaoId,
            IUsuarioInstituicaoService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!await CanManageInstitutionAsync(db, user, instituicaoId)) return Results.Forbid();
            var result = await service.DesvincularUsuarioInstituicaoAsync(usuarioId, instituicaoId);
            return result ? Results.Ok(new { message = "Usuário desvinculado da instituição com sucesso." })
                : Results.NotFound(new { message = "Vínculo não encontrado." });
        }).RequireAuthorization("Gestao");

        route.MapGet("listar-instituicoes-por-usuario", async (Guid usuarioId, IUsuarioInstituicaoService service,
            AppDbContext db, ClaimsPrincipal user) =>
        {
            if (user.IsInRole(nameof(UserRole.Admin)))
                return Results.Ok(new { instituicoes = await service.ListarInstituicoesPorUsuarioAsync(usuarioId) });

            var allowed = await GetManagerInstitutionIdsAsync(db, user);
            var hasManagedLink = await db.UsuariosInstituicoes.AnyAsync(ui =>
                ui.UsuarioId == usuarioId && allowed.Contains(ui.InstituicaoId) && ui.Ativa);
            if (!hasManagedLink) return Results.NotFound();

            var linked = await service.ListarInstituicoesPorUsuarioAsync(usuarioId);
            return Results.Ok(new { instituicoes = linked.Where(allowed.Contains) });
        }).RequireAuthorization("Gestao");

        route.MapGet("listar-usuarios-por-instituicao", async (Guid instituicaoId, IUsuarioInstituicaoService service,
            AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!await CanManageInstitutionAsync(db, user, instituicaoId)) return Results.Forbid();
            return Results.Ok(new { usuarios = await service.ListarUsuariosPorInstituicaoAsync(instituicaoId) });
        }).RequireAuthorization("Gestao");
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    private static async Task<bool> CanManageInstitutionAsync(AppDbContext db, ClaimsPrincipal user, Guid institutionId)
    {
        if (user.IsInRole(nameof(UserRole.Admin)))
            return await db.Instituicoes.AnyAsync(i => i.Id == institutionId && i.Ativa);
        var userId = GetUserId(user);
        return user.IsInRole(nameof(UserRole.Gestor)) && userId != Guid.Empty &&
            await db.UsuariosInstituicoes.AnyAsync(ui => ui.UsuarioId == userId && ui.InstituicaoId == institutionId &&
                ui.Papel == PapelInstituicao.Gestor && ui.Ativa && ui.Instituicao!.Ativa);
    }

    private static async Task<HashSet<Guid>> GetManagerInstitutionIdsAsync(AppDbContext db, ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        if (userId == Guid.Empty) return [];
        return (await db.UsuariosInstituicoes.AsNoTracking()
            .Where(ui => ui.UsuarioId == userId && ui.Papel == PapelInstituicao.Gestor && ui.Ativa && ui.Instituicao!.Ativa)
            .Select(ui => ui.InstituicaoId).Distinct().ToListAsync()).ToHashSet();
    }
}
