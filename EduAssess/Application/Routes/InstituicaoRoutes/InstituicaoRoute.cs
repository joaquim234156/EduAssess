using EduAssess.Application.DTOs.Instituicao;
using EduAssess.Application.Interfaces;

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
            }).RequireAuthorization("Gestao");

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
