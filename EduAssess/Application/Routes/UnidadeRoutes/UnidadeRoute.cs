using System.Security.Claims;
using EduAssess.Application.DTOs.Unidade;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models;
using EduAssess.Models.Global;
using EduAssess.Models.Local;
using EduAssess.Web;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Routes.UnidadeRoutes;

public static class UnidadeRoute
{
    /// <summary>
    /// As rotas são escopadas à instituição. Admin pode atuar em qualquer instituição; Gestor somente
    /// nas instituições em que possui vínculo ativo de Gestão. Leituras de Professor/Aluno só retornam
    /// unidades das instituições às quais o usuário pertence.
    /// </summary>
    public static void UnidadeRoutes(this WebApplication app)
    {
        var route = app.MapGroup("/unidades").WithTags("Unidade");

        route.MapPost("register", async (UnidadeRequest req, IUnidadeService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                return Results.BadRequest(errors);
            if (!await CanManageInstitutionAsync(db, user, req.IdInstituicao))
                return Results.Forbid();

            try
            {
                var result = await service.CriarUnidadeAsync(req);
                return Results.Created($"/unidades/{result.Id}", new { message = "Unidade registrada com sucesso", unidade = result });
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.Conflict(new { message = ex.Message }); }
        }).RequireAuthorization("Gestao");

        route.MapGet("list", async (IUnidadeService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            var items = await service.ListarUnidadesAsync();
            if (user.IsInRole(nameof(UserRole.Admin))) return Results.Ok(items);
            var allowed = await GetMemberInstitutionIdsAsync(db, user);
            return Results.Ok(items.Where(x => allowed.Contains(x.InstituicaoId)));
        }).RequireAuthorization("Gestao");

        route.MapGet("list/ativos", async (IUnidadeService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            var items = await service.ListarUnidadesAtivasAsync();
            if (user.IsInRole(nameof(UserRole.Admin))) return Results.Ok(items);
            var allowed = await GetMemberInstitutionIdsAsync(db, user);
            return Results.Ok(items.Where(x => allowed.Contains(x.InstituicaoId)));
        }).RequireAuthorization();

        route.MapGet("list/inativos", async (IUnidadeService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            var allowed = await GetManageableInstitutionIdsAsync(db, user);
            var items = await service.ListarUnidadesInativasAsync();
            return Results.Ok(user.IsInRole(nameof(UserRole.Admin)) ? items : items.Where(x => allowed.Contains(x.InstituicaoId)));
        }).RequireAuthorization("Gestao");

        route.MapGet("{id:guid}", async (Guid id, IUnidadeService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            try
            {
                var unit = await service.ObterUnidadePorIdAsync(id);
                if (!await CanReadInstitutionAsync(db, user, unit.InstituicaoId)) return Results.NotFound();
                return Results.Ok(unit);
            }
            catch (KeyNotFoundException) { return Results.NotFound(new { message = "Unidade não encontrada." }); }
        }).RequireAuthorization();

        route.MapGet("list/instituicao/{instituicaoId:guid}", async (Guid instituicaoId, IUnidadeService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!await CanReadInstitutionAsync(db, user, instituicaoId)) return Results.NotFound();
            return Results.Ok(await service.ListarUnidadesPorInstituicaoAsync(instituicaoId));
        }).RequireAuthorization();

        route.MapGet("list/tipo/{tipo}", async (TipoUnidade tipo, IUnidadeService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!Enum.IsDefined(tipo)) return Results.BadRequest(new { message = "Tipo de unidade inválido." });
            var items = await service.ListarUnidadesPorTipoAsync(tipo);
            if (user.IsInRole(nameof(UserRole.Admin))) return Results.Ok(items);
            var allowed = await GetMemberInstitutionIdsAsync(db, user);
            return Results.Ok(items.Where(x => allowed.Contains(x.InstituicaoId)));
        }).RequireAuthorization();

        route.MapGet("list/instituicao/{instituicaoId:guid}/tipo/{tipo}", async (Guid instituicaoId, TipoUnidade tipo, IUnidadeService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!Enum.IsDefined(tipo)) return Results.BadRequest(new { message = "Tipo de unidade inválido." });
            if (!await CanReadInstitutionAsync(db, user, instituicaoId)) return Results.NotFound();
            return Results.Ok(await service.ListarUnidadesPorInstituicaoETipoAsync(instituicaoId, tipo));
        }).RequireAuthorization();

        route.MapPut("atualizar/{id:guid}", async (Guid id, UnidadeRequestPut req, IUnidadeService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                return Results.BadRequest(errors);
            var institutionId = await GetUnitInstitutionIdAsync(db, id);
            if (institutionId is null) return Results.NotFound(new { message = "Unidade não encontrada." });
            if (!await CanManageInstitutionAsync(db, user, institutionId.Value)) return Results.Forbid();
            try { return Results.Ok(await service.AtualizarUnidadeAsync(id, req)); }
            catch (KeyNotFoundException) { return Results.NotFound(new { message = "Unidade não encontrada." }); }
            catch (ArgumentException ex) { return Results.Conflict(new { message = ex.Message }); }
        }).RequireAuthorization("Gestao");

        route.MapPut("{id:guid}/identidade", async (Guid id, UnidadeIdentidadeRequest req, IUnidadeService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            var institutionId = await GetUnitInstitutionIdAsync(db, id);
            if (institutionId is null) return Results.NotFound(new { message = "Unidade não encontrada." });
            if (!await CanManageInstitutionAsync(db, user, institutionId.Value)) return Results.Forbid();
            try { return Results.Ok(await service.AtualizarIdentidadeAsync(id, req.CorPrimaria, req.CorSecundaria)); }
            catch (KeyNotFoundException) { return Results.NotFound(new { message = "Unidade não encontrada." }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).RequireAuthorization("Gestao");

        route.MapPost("{id:guid}/logo", async (Guid id, IFormFile arquivo, IUnidadeService service, AppDbContext db,
            ClaimsPrincipal user, ImageStorageService images, CancellationToken ct) =>
        {
            var institutionId = await GetUnitInstitutionIdAsync(db, id);
            if (institutionId is null) return Results.NotFound(new { message = "Unidade não encontrada." });
            if (!await CanManageInstitutionAsync(db, user, institutionId.Value)) return Results.Forbid();
            if (arquivo is null || arquivo.Length == 0)
                return Results.BadRequest(new { message = "Selecione uma imagem." });
            if (arquivo.Length > ImageStorageService.MaxUploadBytes)
                return Results.BadRequest(new { message = "A imagem passa de 8 MB." });
            try
            {
                await using var stream = arquivo.OpenReadStream();
                await images.SaveUnitLogoAsync(id, stream, ct);
                return Results.Ok(await service.DefinirLogoAsync(id, true));
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (KeyNotFoundException) { images.DeleteUnitLogo(id); return Results.NotFound(new { message = "Unidade não encontrada." }); }
        }).RequireAuthorization("Gestao").DisableAntiforgery();

        route.MapDelete("{id:guid}/logo", async (Guid id, IUnidadeService service, AppDbContext db, ClaimsPrincipal user, ImageStorageService images) =>
        {
            var institutionId = await GetUnitInstitutionIdAsync(db, id);
            if (institutionId is null) return Results.NotFound(new { message = "Unidade não encontrada." });
            if (!await CanManageInstitutionAsync(db, user, institutionId.Value)) return Results.Forbid();
            try
            {
                var result = await service.DefinirLogoAsync(id, false);
                images.DeleteUnitLogo(id);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(new { message = "Unidade não encontrada." }); }
        }).RequireAuthorization("Gestao");

        route.MapPut("{id:guid}/ativar", async (Guid id, IUnidadeService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            var institutionId = await GetUnitInstitutionIdAsync(db, id);
            if (institutionId is null) return Results.NotFound(new { message = "Unidade não encontrada." });
            if (!await CanManageInstitutionAsync(db, user, institutionId.Value)) return Results.Forbid();
            return await service.AtivarUnidadeAsync(id)
                ? Results.Ok(new { message = "Unidade ativada com sucesso." })
                : Results.NotFound(new { message = "Unidade não encontrada." });
        }).RequireAuthorization("Gestao");

        route.MapDelete("{id:guid}", async (Guid id, IUnidadeService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            var institutionId = await GetUnitInstitutionIdAsync(db, id);
            if (institutionId is null) return Results.NotFound(new { message = "Unidade não encontrada." });
            if (!await CanManageInstitutionAsync(db, user, institutionId.Value)) return Results.Forbid();
            try
            {
                await service.DeletarUnidadeAsync(id);
                return Results.Ok(new { message = "Unidade desativada com sucesso." });
            }
            catch (KeyNotFoundException) { return Results.NotFound(new { message = "Unidade não encontrada." }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { message = ex.Message }); }
        }).RequireAuthorization("Gestao");
    }

    private static async Task<Guid?> GetUnitInstitutionIdAsync(AppDbContext db, Guid unitId) =>
        await db.Unidades.AsNoTracking().Where(u => u.Id == unitId).Select(u => (Guid?)u.InstituicaoId).FirstOrDefaultAsync();

    private static async Task<bool> CanReadInstitutionAsync(AppDbContext db, ClaimsPrincipal user, Guid institutionId)
    {
        if (user.IsInRole(nameof(UserRole.Admin))) return await db.Instituicoes.AnyAsync(i => i.Id == institutionId && i.Ativa);
        var userId = GetUserId(user);
        return userId != Guid.Empty && await db.UsuariosInstituicoes.AnyAsync(ui =>
            ui.UsuarioId == userId && ui.InstituicaoId == institutionId && ui.Ativa && ui.Instituicao!.Ativa);
    }

    private static async Task<bool> CanManageInstitutionAsync(AppDbContext db, ClaimsPrincipal user, Guid institutionId)
    {
        if (user.IsInRole(nameof(UserRole.Admin))) return await db.Instituicoes.AnyAsync(i => i.Id == institutionId && i.Ativa);
        var userId = GetUserId(user);
        return user.IsInRole(nameof(UserRole.Gestor)) && userId != Guid.Empty &&
            await db.UsuariosInstituicoes.AnyAsync(ui => ui.UsuarioId == userId && ui.InstituicaoId == institutionId &&
                ui.Papel == PapelInstituicao.Gestor && ui.Ativa && ui.Instituicao!.Ativa);
    }

    private static async Task<HashSet<Guid>> GetMemberInstitutionIdsAsync(AppDbContext db, ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        if (userId == Guid.Empty) return [];
        return (await db.UsuariosInstituicoes.AsNoTracking()
            .Where(ui => ui.UsuarioId == userId && ui.Ativa && ui.Instituicao!.Ativa)
            .Select(ui => ui.InstituicaoId).Distinct().ToListAsync()).ToHashSet();
    }

    private static async Task<HashSet<Guid>> GetManageableInstitutionIdsAsync(AppDbContext db, ClaimsPrincipal user)
    {
        if (user.IsInRole(nameof(UserRole.Admin)))
            return (await db.Instituicoes.AsNoTracking().Where(i => i.Ativa).Select(i => i.Id).ToListAsync()).ToHashSet();
        var userId = GetUserId(user);
        if (userId == Guid.Empty) return [];
        return (await db.UsuariosInstituicoes.AsNoTracking()
            .Where(ui => ui.UsuarioId == userId && ui.Papel == PapelInstituicao.Gestor && ui.Ativa && ui.Instituicao!.Ativa)
            .Select(ui => ui.InstituicaoId).Distinct().ToListAsync()).ToHashSet();
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;
}
