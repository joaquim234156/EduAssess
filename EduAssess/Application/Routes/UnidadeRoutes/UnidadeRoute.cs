using EduAssess.Application.DTOs.Unidade;
using EduAssess.Application.Interfaces;
using EduAssess.Models.Global;

namespace EduAssess.Application.Routes.UnidadeRoutes
{
    public static class UnidadeRoute
    {
        // CORREÇÕES desta versão:
        //  1) Segurança: só o "register" exigia login; listar, buscar, EXCLUIR e reativar aceitavam
        //     qualquer pessoa sem login. Agora leituras exigem login e alterações exigem a Gestão.
        //  2) "atualizar" exigia a policy "Professor" (um professor não deveria editar prédios): agora "Gestao".
        //  3) "register" comparava o ID do usuário com o ID da instituição (nunca fazia sentido); removido.
        //  4) Erros de regra (não achou, nome repetido, unidade com gente) viram 404/409 em vez de 500.
        public static void UnidadeRoutes(this WebApplication app)
        {
            var route = app.MapGroup("/unidades").WithTags("Unidade");

            route.MapPost("register", async (UnidadeRequest req, IUnidadeService unidadeService) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);

                try
                {
                    var resultado = await unidadeService.CriarUnidadeAsync(req);
                    return Results.Ok(new
                    {
                        message = "Unidade registrada com sucesso",
                        unidade = resultado
                    });
                }
                catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
                catch (ArgumentException ex) { return Results.Conflict(new { message = ex.Message }); }
            }).RequireAuthorization("Gestao");

            route.MapGet("list", async (IUnidadeService unidadeService) =>
                Results.Ok(await unidadeService.ListarUnidadesAsync())).RequireAuthorization("Gestao");

            route.MapGet("list/ativos", async (IUnidadeService unidadeService) =>
                Results.Ok(await unidadeService.ListarUnidadesAtivasAsync())).RequireAuthorization();

            route.MapGet("list/inativos", async (IUnidadeService unidadeService) =>
                Results.Ok(await unidadeService.ListarUnidadesInativasAsync())).RequireAuthorization("Gestao");

            route.MapGet("{id:guid}", async (Guid id, IUnidadeService unidadeService) =>
            {
                try
                {
                    return Results.Ok(await unidadeService.ObterUnidadePorIdAsync(id));
                }
                catch (KeyNotFoundException)
                {
                    return Results.NotFound(new { message = "Unidade não encontrada." });
                }
            }).RequireAuthorization();

            route.MapGet("list/instituicao/{instituicaoId:guid}", async (Guid instituicaoId, IUnidadeService unidadeService) =>
                Results.Ok(await unidadeService.ListarUnidadesPorInstituicaoAsync(instituicaoId))).RequireAuthorization();

            route.MapGet("list/tipo/{tipo}", async (TipoUnidade tipo, IUnidadeService unidadeService) =>
                Results.Ok(await unidadeService.ListarUnidadesPorTipoAsync(tipo))).RequireAuthorization();

            route.MapGet("list/instituicao/{instituicaoId:guid}/tipo/{tipo}", async (Guid instituicaoId, TipoUnidade tipo, IUnidadeService unidadeService) =>
                Results.Ok(await unidadeService.ListarUnidadesPorInstituicaoETipoAsync(instituicaoId, tipo))).RequireAuthorization();

            route.MapPut("atualizar/{id:guid}", async (Guid id, UnidadeRequestPut req, IUnidadeService unidadeService) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);

                try
                {
                    return Results.Ok(await unidadeService.AtualizarUnidadeAsync(id, req));
                }
                catch (KeyNotFoundException)
                {
                    return Results.NotFound(new { message = "Unidade não encontrada." });
                }
                catch (ArgumentException ex) { return Results.Conflict(new { message = ex.Message }); }
            }).RequireAuthorization("Gestao");

            route.MapPut("{id:guid}/ativar", async (Guid id, IUnidadeService unidadeService) =>
            {
                var result = await unidadeService.AtivarUnidadeAsync(id);
                if (!result)
                {
                    return Results.NotFound(new { message = "Unidade não encontrada." });
                }
                return Results.Ok(new { message = "Unidade ativada com sucesso." });
            }).RequireAuthorization("Gestao");

            route.MapDelete("{id:guid}", async (Guid id, IUnidadeService unidadeService) =>
            {
                try
                {
                    await unidadeService.DeletarUnidadeAsync(id);
                    return Results.Ok(new { message = "Unidade deletada com sucesso." });
                }
                catch (KeyNotFoundException)
                {
                    return Results.NotFound(new { message = "Unidade não encontrada." });
                }
                catch (InvalidOperationException ex) { return Results.Conflict(new { message = ex.Message }); }
            }).RequireAuthorization("Gestao");
        }
    }
}
