using EduAssess.Application.DTOs.Disciplina;
using EduAssess.Application.DTOs.Instituicao;
using EduAssess.Application.Interfaces;
using System.Security.Claims;

namespace EduAssess.Application.Routes.InstituicaoRoutes
{
    public static class InstituicaoRoute
    {
        public static void InstituicaoRoutes(this WebApplication app)
        {
            var route = app.MapGroup("/instituicoes").WithTags("Instituicao");

            route.MapPost("register", async (InstituicaoRequest req, IInstituicaoService instituicaoService) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);
                var resultado = await instituicaoService.CriarInstituicaoAsync(req);
                return Results.Ok(new
                {
                    message = "Instituição registrada com sucesso",
                    instituicao = resultado
                });
            }).RequireAuthorization("Gestao");

            route.MapGet("list", async (IInstituicaoService instituicaoService) =>
            {
                var instituicoes = await instituicaoService.ListarInstituicoesAsync();
                return Results.Ok(instituicoes);
            });

            route.MapGet("list/ativos", async (IInstituicaoService instituicaoService) =>
            {
                var instituicoes = await instituicaoService.ListarInstituicoesAtivasAsync();
                return Results.Ok(instituicoes);
            });

            route.MapGet("{id:guid}", async (Guid id, IInstituicaoService instituicaoService) =>
            {
                var instituicao = await instituicaoService.ObterInstituicaoPorIdAsync(id);
                if (instituicao == null)
                {
                    return Results.NotFound(new { message = "Instituição não encontrada." });
                }
                return Results.Ok(instituicao);
            });

            route.MapDelete("{id:guid}", async (Guid id, IInstituicaoService instituicaoService) =>
            {
                var result = await instituicaoService.DeletarInstituicaoAsync(id);
                if (!result)
                {
                    return Results.NotFound(new { message = "Instituição não encontrada." });
                }
                return Results.Ok(new { message = "Instituição deletada com sucesso." });
            });

            route.MapGet("list/inativos", async (IInstituicaoService instituicaoService) =>
            {
                var instituicoes = await instituicaoService.ListarInstituicoesInativasAsync();
                return Results.Ok(instituicoes);
            });

            route.MapGet("{id:guid}/usuarios", async (Guid id, IUsuarioInstituicaoService usuarioInstituicaoService) =>
            {
                var usuarios = await usuarioInstituicaoService.ListarUsuariosPorInstituicaoAsync(id);
                return Results.Ok(usuarios);
            });

            route.MapGet("{id:guid}/unidades", async (Guid id, IUnidadeService unidadeService) =>
            {
                var unidades = await unidadeService.ListarUnidadesPorInstituicaoAsync(id);
                return Results.Ok(unidades);
            });

            route.MapPut("{id:guid}/ativar", async (Guid id, IInstituicaoService instituicaoService) =>
            {
                var result = await instituicaoService.AtivarInstituicaoAsync(id);
                if (!result)
                {
                    return Results.NotFound(new { message = "Instituição não encontrada." });
                }
                return Results.Ok(new { message = "Instituição ativada com sucesso." });
            });

            route.MapPut("{id:guid}", async (Guid id, InstituicaoRequestPut req, IInstituicaoService instituicaoService) =>
            {
                try
                {
                    var updatedInstituicao = await instituicaoService.AtualizarInstituicaoAsync(id, req);
                    return Results.Ok(updatedInstituicao);
                }
                catch (KeyNotFoundException)
                {
                    return Results.NotFound(new { message = "Instituição não encontrada." });
                }
            });
        }
    }
}