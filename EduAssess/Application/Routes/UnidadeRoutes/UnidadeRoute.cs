using EduAssess.Application.DTOs.Nota;
using EduAssess.Application.DTOs.Unidade;
using EduAssess.Application.Interfaces;
using EduAssess.Models.Global;
using System.Security.Claims;

namespace EduAssess.Application.Routes.UnidadeRoutes
{
    public static class UnidadeRoute
    {
        public static void UnidadeRoutes(this WebApplication app)
        {
            var route = app.MapGroup("/unidades").WithTags("Unidade");

            route.MapPost("register", async (UnidadeRequest req, IUnidadeService unidadeService, ClaimsPrincipal principal) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);
                var currentUserIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (principal.IsInRole("Professor") && currentUserIdStr != req.IdInstituicao.ToString())
                {
                    return Results.Forbid();
                }
                try
                {
                    var resultado = await unidadeService.CriarUnidadeAsync(req);
                    return Results.Ok(new
                    {
                        message = "Unidade registrada com sucesso",
                        unidade = resultado
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            }).RequireAuthorization("Gestao");

            route.MapGet("list", async (IUnidadeService unidadeService) =>
            {
                var unidades = await unidadeService.ListarUnidadesAsync();
                return Results.Ok(unidades);
            });

            route.MapGet("list/ativos", async (IUnidadeService unidadeService) =>
            {
                var unidades = await unidadeService.ListarUnidadesAtivasAsync();
                return Results.Ok(unidades);
            });

            route.MapGet("{id:guid}", async (Guid id, IUnidadeService unidadeService) =>
            {
                var unidade = await unidadeService.ObterUnidadePorIdAsync(id);
                if (unidade == null)
                {
                    return Results.NotFound(new { message = "Unidade não encontrada." });
                }
                return Results.Ok(unidade);
            });

            route.MapDelete("{id:guid}", async (Guid id, IUnidadeService unidadeService) =>
            {
                var result = await unidadeService.DeletarUnidadeAsync(id);
                if (!result)
                {
                    return Results.NotFound(new { message = "Unidade não encontrada." });
                }
                return Results.Ok(new { message = "Unidade deletada com sucesso." });
            });

            route.MapGet("list/inativos", async (IUnidadeService unidadeService) =>
            {
                var unidades = await unidadeService.ListarUnidadesInativasAsync();
                return Results.Ok(unidades);
            });

            route.MapPut("{id:guid}/ativar", async (Guid id, IUnidadeService unidadeService) =>
            {
                var result = await unidadeService.AtivarUnidadeAsync(id);
                if (!result)
                {
                    return Results.NotFound(new { message = "Unidade não encontrada." });
                }
                return Results.Ok(new { message = "Unidade ativada com sucesso." });
            });

            route.MapGet("list/instituicao/{instituicaoId:guid}", async (Guid instituicaoId, IUnidadeService unidadeService) =>
            {
                var unidades = await unidadeService.ListarUnidadesPorInstituicaoAsync(instituicaoId);
                return Results.Ok(unidades);
            });

            route.MapGet("list/tipo/{tipo}", async (TipoUnidade tipo, IUnidadeService unidadeService) =>
            {
                var unidades = await unidadeService.ListarUnidadesPorTipoAsync(tipo);
                return Results.Ok(unidades);
            });

            route.MapGet("list/instituicao/{instituicaoId:guid}/tipo/{tipo}", async (Guid instituicaoId, TipoUnidade tipo, IUnidadeService unidadeService) =>
            {
                var unidades = await unidadeService.ListarUnidadesPorInstituicaoETipoAsync(instituicaoId, tipo);
                return Results.Ok(unidades);
            });

            route.MapPut("atualizar/{id:guid}", async (Guid id, UnidadeRequestPut req, IUnidadeService unidadeService) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);
                var unidadeAtualizada = await unidadeService.AtualizarUnidadeAsync(id, req);
                return Results.Ok(unidadeAtualizada);
            }).RequireAuthorization("Professor");
        }
    }
}
