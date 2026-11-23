using System.Security.Claims;
using EduAssess.Application.DTOs.Instituicao;
using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models.Local;
using EduAssess.Models.Global;
using EduAssess.Web;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Routes.InstituicaoRoutes;

public static class InstituicaoRoute
{
    /// <summary>
    /// Escopo multi-instituição: Admin da plataforma pode administrar todos os tenants;
    /// Gestor só pode operar nas instituições em que possui vínculo ativo de Gestão.
    /// A relação por mantenedor não concede acesso aos dados acadêmicos das instituições irmãs.
    /// </summary>
    public static void InstituicaoRoutes(this WebApplication app)
    {
        var route = app.MapGroup("/instituicoes").WithTags("Instituicao");

        route.MapPost("register", async (InstituicaoRequest req, IInstituicaoService service) =>
        {
            if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                return Results.BadRequest(errors);
            try
            {
                var created = await service.CriarInstituicaoAsync(req);
                return Results.Created($"/instituicoes/{created.Id}", new { message = "Instituição registrada com sucesso", instituicao = created });
            }
            catch (ArgumentException ex) { return Results.Conflict(new { message = ex.Message }); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
        }).RequireAuthorization("Admin");

        route.MapGet("list", async (IInstituicaoService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            var items = await service.ListarInstituicoesAsync();
            if (user.IsInRole(nameof(UserRole.Admin))) return Results.Ok(items);
            var allowed = await GetManagerInstitutionIdsAsync(db, user);
            return Results.Ok(items.Where(x => allowed.Contains(x.Id)));
        }).RequireAuthorization("Gestao");

        route.MapGet("list/ativos", async (IInstituicaoService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            var items = await service.ListarInstituicoesAtivasAsync();
            if (user.IsInRole(nameof(UserRole.Admin))) return Results.Ok(items);
            var allowed = await GetMemberInstitutionIdsAsync(db, user);
            return Results.Ok(items.Where(x => allowed.Contains(x.Id)));
        }).RequireAuthorization();

        route.MapGet("list/inativos", async (IInstituicaoService service, ClaimsPrincipal user) =>
        {
            if (!user.IsInRole(nameof(UserRole.Admin))) return Results.Forbid();
            return Results.Ok(await service.ListarInstituicoesInativasAsync());
        }).RequireAuthorization("Gestao");

        route.MapGet("{id:guid}", async (Guid id, IInstituicaoService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!await CanReadInstitutionAsync(db, user, id)) return Results.NotFound(new { message = "Instituição não encontrada." });
            try { return Results.Ok(await service.ObterInstituicaoPorIdAsync(id)); }
            catch (KeyNotFoundException) { return Results.NotFound(new { message = "Instituição não encontrada." }); }
        }).RequireAuthorization();

        route.MapPut("{id:guid}", async (Guid id, InstituicaoRequestPut req, IInstituicaoService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                return Results.BadRequest(errors);
            if (!await CanManageInstitutionAsync(db, user, id)) return Results.Forbid();

            // Apenas Admin da plataforma pode alterar categoria e mantenedor. Gestor altera os dados
            // cadastrais da própria instituição, preservando esses dois campos mesmo que envie valores.
            var safeRequest = user.IsInRole(nameof(UserRole.Admin))
                ? req
                : req with { Tipo = null, MantenedorId = null, MantenedorNome = null, LimparMantenedor = false };

            try { return Results.Ok(await service.AtualizarInstituicaoAsync(id, safeRequest)); }
            catch (KeyNotFoundException) { return Results.NotFound(new { message = "Instituição não encontrada." }); }
            catch (ArgumentException ex) { return Results.Conflict(new { message = ex.Message }); }
        }).RequireAuthorization("Gestao");

        // Grupo/rede: Admin lista todos os mantenedores; Gestor vê apenas o mantenedor da instituição
        // à qual está vinculado. Os dados retornados são somente cadastrais, não dados acadêmicos.
        route.MapGet("mantenedores", async (IInstituicaoService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            var items = await service.ListarMantenedoresAsync();
            if (user.IsInRole(nameof(UserRole.Admin))) return Results.Ok(items);
            var allowed = await GetMemberInstitutionIdsAsync(db, user);
            var maintainerIds = await db.Instituicoes.AsNoTracking()
                .Where(i => allowed.Contains(i.Id) && i.MantenedorId != null)
                .Select(i => i.MantenedorId!.Value).Distinct().ToListAsync();
            return Results.Ok(items.Where(x => maintainerIds.Contains(x.Id)));
        }).RequireAuthorization("Gestao");

        route.MapPut("{id:guid}/identidade", async (Guid id, InstituicaoIdentidadeRequest req, IInstituicaoService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!await CanManageInstitutionAsync(db, user, id)) return Results.Forbid();
            try { return Results.Ok(await service.AtualizarIdentidadeAsync(id, req)); }
            catch (KeyNotFoundException) { return Results.NotFound(new { message = "Instituição não encontrada." }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).RequireAuthorization("Gestao");

        route.MapPost("{id:guid}/logo", async (Guid id, IFormFile arquivo, IInstituicaoService service, AppDbContext db,
            ClaimsPrincipal user, ImageStorageService images, CancellationToken ct) =>
        {
            if (!await CanManageInstitutionAsync(db, user, id)) return Results.Forbid();
            if (arquivo is null || arquivo.Length == 0)
                return Results.BadRequest(new { message = "Selecione uma imagem." });
            if (arquivo.Length > ImageStorageService.MaxUploadBytes)
                return Results.BadRequest(new { message = "A imagem passa de 8 MB." });
            try
            {
                await using var stream = arquivo.OpenReadStream();
                await images.SaveLogoAsync(id, stream, ct);
                try { return Results.Ok(await service.DefinirLogoAsync(id, true)); }
                catch { images.DeleteLogo(id); throw; }
            }
            catch (KeyNotFoundException) { return Results.NotFound(new { message = "Instituição não encontrada." }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).RequireAuthorization("Gestao").DisableAntiforgery();

        route.MapDelete("{id:guid}/logo", async (Guid id, IInstituicaoService service, AppDbContext db,
            ClaimsPrincipal user, ImageStorageService images) =>
        {
            if (!await CanManageInstitutionAsync(db, user, id)) return Results.Forbid();
            try
            {
                var result = await service.DefinirLogoAsync(id, false);
                images.DeleteLogo(id);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(new { message = "Instituição não encontrada." }); }
        }).RequireAuthorization("Gestao");

        route.MapGet("{id:guid}/logo", async (Guid id, ImageStorageService images, AppDbContext db, ClaimsPrincipal user, HttpContext http) =>
        {
            if (!await CanReadInstitutionAsync(db, user, id)) return Results.NotFound();
            var path = images.LogoPath(id);
            if (!File.Exists(path)) return Results.NotFound();
            http.Response.Headers.CacheControl = "private, max-age=3600";
            return Results.File(path, "image/webp");
        }).RequireAuthorization();

        route.MapDelete("{id:guid}", async (Guid id, IInstituicaoService service) =>
        {
            var result = await service.DeletarInstituicaoAsync(id);
            return result ? Results.Ok(new { message = "Instituição desativada com sucesso." }) : Results.NotFound(new { message = "Instituição não encontrada." });
        }).RequireAuthorization("Admin");

        route.MapPut("{id:guid}/ativar", async (Guid id, IInstituicaoService service) =>
        {
            var result = await service.AtivarInstituicaoAsync(id);
            return result ? Results.Ok(new { message = "Instituição ativada com sucesso." }) : Results.NotFound(new { message = "Instituição não encontrada." });
        }).RequireAuthorization("Admin");

        route.MapGet("{id:guid}/usuarios", async (Guid id, IUsuarioInstituicaoService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!await CanManageInstitutionAsync(db, user, id)) return Results.NotFound();
            return Results.Ok(await service.ListarUsuariosPorInstituicaoAsync(id));
        }).RequireAuthorization("Gestao");

        route.MapGet("{id:guid}/unidades", async (Guid id, IUnidadeService service, AppDbContext db, ClaimsPrincipal user) =>
        {
            if (!await CanReadInstitutionAsync(db, user, id)) return Results.NotFound();
            return Results.Ok(await service.ListarUnidadesPorInstituicaoAsync(id));
        }).RequireAuthorization();
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    private static async Task<bool> CanReadInstitutionAsync(AppDbContext db, ClaimsPrincipal user, Guid institutionId)
    {
        if (user.IsInRole(nameof(UserRole.Admin)))
            return await db.Instituicoes.AnyAsync(i => i.Id == institutionId && i.Ativa);
        var userId = GetUserId(user);
        return userId != Guid.Empty && await db.UsuariosInstituicoes.AnyAsync(ui =>
            ui.UsuarioId == userId && ui.InstituicaoId == institutionId && ui.Ativa && ui.Instituicao!.Ativa);
    }

    private static async Task<bool> CanManageInstitutionAsync(AppDbContext db, ClaimsPrincipal user, Guid institutionId)
    {
        if (user.IsInRole(nameof(UserRole.Admin)))
            return await db.Instituicoes.AnyAsync(i => i.Id == institutionId && i.Ativa);
        var userId = GetUserId(user);
        return user.IsInRole(nameof(UserRole.Gestor)) && userId != Guid.Empty && await db.UsuariosInstituicoes.AnyAsync(ui =>
            ui.UsuarioId == userId && ui.InstituicaoId == institutionId && ui.Papel == PapelInstituicao.Gestor && ui.Ativa && ui.Instituicao!.Ativa);
    }

    private static async Task<HashSet<Guid>> GetMemberInstitutionIdsAsync(AppDbContext db, ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        if (userId == Guid.Empty) return [];
        return (await db.UsuariosInstituicoes.AsNoTracking()
            .Where(ui => ui.UsuarioId == userId && ui.Ativa && ui.Instituicao!.Ativa)
            .Select(ui => ui.InstituicaoId).Distinct().ToListAsync()).ToHashSet();
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
