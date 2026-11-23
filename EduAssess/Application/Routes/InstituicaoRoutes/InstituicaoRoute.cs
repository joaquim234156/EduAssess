using EduAssess.Application.DTOs.Instituicao;
using EduAssess.Application.Interfaces;
using EduAssess.Web;

namespace EduAssess.Application.Routes.InstituicaoRoutes
{
    public static class InstituicaoRoute
    {
        // CORREÇÃO DE SEGURANÇA: nesta versão só o "register" exigia login. As demais rotas, inclusive
        // DELETE, ativar e atualizar, aceitavam QUALQUER pessoa, mesmo sem login. Agora:
        //   - leituras simples: qualquer usuário autenticado;
        //   - listas completas e lista de usuários: Gestão (Admin ou Gestor);
        //   - criar e atualizar: Gestão; excluir e reativar: somente Admin.
        public static void InstituicaoRoutes(this WebApplication app)
        {
            var route = app.MapGroup("/instituicoes").WithTags("Instituicao");

            route.MapPost("register", async (InstituicaoRequest req, IInstituicaoService instituicaoService) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);

                try
                {
                    var resultado = await instituicaoService.CriarInstituicaoAsync(req);
                    return Results.Ok(new
                    {
                        message = "Instituição registrada com sucesso",
                        instituicao = resultado
                    });
                }
                catch (ArgumentException ex) { return Results.Conflict(new { message = ex.Message }); }
                catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
            }).RequireAuthorization("Admin");   // ALTERADO: criar uma instituição nova (novo "inquilino") ficou só com o Admin

            route.MapGet("list", async (IInstituicaoService instituicaoService) =>
            {
                var instituicoes = await instituicaoService.ListarInstituicoesAsync();
                return Results.Ok(instituicoes);
            }).RequireAuthorization("Gestao");

            route.MapGet("list/ativos", async (IInstituicaoService instituicaoService) =>
            {
                var instituicoes = await instituicaoService.ListarInstituicoesAtivasAsync();
                return Results.Ok(instituicoes);
            }).RequireAuthorization();

            route.MapGet("list/inativos", async (IInstituicaoService instituicaoService) =>
            {
                var instituicoes = await instituicaoService.ListarInstituicoesInativasAsync();
                return Results.Ok(instituicoes);
            }).RequireAuthorization("Gestao");

            // ALTERADO: o serviço lança KeyNotFoundException quando não acha; antes isso virava 500.
            route.MapGet("{id:guid}", async (Guid id, IInstituicaoService instituicaoService) =>
            {
                try
                {
                    return Results.Ok(await instituicaoService.ObterInstituicaoPorIdAsync(id));
                }
                catch (KeyNotFoundException)
                {
                    return Results.NotFound(new { message = "Instituição não encontrada." });
                }
            }).RequireAuthorization();

            route.MapPut("{id:guid}", async (Guid id, InstituicaoRequestPut req, IInstituicaoService instituicaoService) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);

                try
                {
                    var updatedInstituicao = await instituicaoService.AtualizarInstituicaoAsync(id, req);
                    return Results.Ok(updatedInstituicao);
                }
                catch (KeyNotFoundException)
                {
                    return Results.NotFound(new { message = "Instituição não encontrada." });
                }
                catch (ArgumentException ex) { return Results.Conflict(new { message = ex.Message }); }
            }).RequireAuthorization("Gestao");

            // ───────────── NOVO: mantenedor, identidade visual e logotipo ─────────────
            route.MapGet("mantenedores", async (IInstituicaoService svc) => Results.Ok(await svc.ListarMantenedoresAsync()))
                .RequireAuthorization("Gestao");

            route.MapPut("{id:guid}/identidade", async (Guid id, InstituicaoIdentidadeRequest req, IInstituicaoService svc) =>
            {
                try { return Results.Ok(await svc.AtualizarIdentidadeAsync(id, req)); }
                catch (KeyNotFoundException) { return Results.NotFound(new { message = "Instituição não encontrada." }); }
                catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
            }).RequireAuthorization("Gestao");

            // Corpo multipart/form-data com o campo "arquivo". JWT não usa cookie, então o antiforgery não se aplica.
            route.MapPost("{id:guid}/logo", async (Guid id, IFormFile arquivo, IInstituicaoService svc, ImageStorageService images, CancellationToken ct) =>
            {
                try
                {
                    await svc.ObterInstituicaoPorIdAsync(id);
                    if (arquivo.Length > ImageStorageService.MaxUploadBytes)
                        return Results.BadRequest(new { message = "A imagem passa de 8 MB." });
                    await using var stream = arquivo.OpenReadStream();
                    await images.SaveLogoAsync(id, stream, ct);
                    return Results.Ok(await svc.DefinirLogoAsync(id, true));
                }
                catch (KeyNotFoundException) { return Results.NotFound(new { message = "Instituição não encontrada." }); }
                catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
            }).RequireAuthorization("Gestao").DisableAntiforgery();

            route.MapDelete("{id:guid}/logo", async (Guid id, IInstituicaoService svc, ImageStorageService images) =>
            {
                try
                {
                    var result = await svc.DefinirLogoAsync(id, false);
                    images.DeleteLogo(id);
                    return Results.Ok(result);
                }
                catch (KeyNotFoundException) { return Results.NotFound(new { message = "Instituição não encontrada." }); }
            }).RequireAuthorization("Gestao");

            route.MapGet("{id:guid}/logo", (Guid id, ImageStorageService images, HttpContext http) =>
            {
                var path = images.LogoPath(id);
                if (!File.Exists(path)) return Results.NotFound();
                http.Response.Headers.CacheControl = "private, max-age=3600";
                return Results.File(path, "image/webp");
            }).RequireAuthorization();

            route.MapDelete("{id:guid}", async (Guid id, IInstituicaoService instituicaoService) =>
            {
                var result = await instituicaoService.DeletarInstituicaoAsync(id);
                if (!result)
                {
                    return Results.NotFound(new { message = "Instituição não encontrada." });
                }
                return Results.Ok(new { message = "Instituição deletada com sucesso." });
            }).RequireAuthorization("Admin");

            route.MapPut("{id:guid}/ativar", async (Guid id, IInstituicaoService instituicaoService) =>
            {
                var result = await instituicaoService.AtivarInstituicaoAsync(id);
                if (!result)
                {
                    return Results.NotFound(new { message = "Instituição não encontrada." });
                }
                return Results.Ok(new { message = "Instituição ativada com sucesso." });
            }).RequireAuthorization("Admin");

            // Devolve IDs de pessoas: só a Gestão pode ver.
            route.MapGet("{id:guid}/usuarios", async (Guid id, IUsuarioInstituicaoService usuarioInstituicaoService) =>
            {
                var usuarios = await usuarioInstituicaoService.ListarUsuariosPorInstituicaoAsync(id);
                return Results.Ok(usuarios);
            }).RequireAuthorization("Gestao");

            route.MapGet("{id:guid}/unidades", async (Guid id, IUnidadeService unidadeService) =>
            {
                var unidades = await unidadeService.ListarUnidadesPorInstituicaoAsync(id);
                return Results.Ok(unidades);
            }).RequireAuthorization();
        }
    }
}
