using EduAssess.Application.DTOs.UsuarioInstituicao;
using EduAssess.Application.Interfaces;

namespace EduAssess.Application.Routes.UsuarioInstituicaoRoutes
{
    public static class UsuarioInstituicaoRoute
    {
        public static void UsuarioInstituicaoRoutes(this WebApplication app)
        {
            var route = app.MapGroup("/usuario-instituicao").WithTags("UsuarioInstituicao");

            route.MapPost("vincular", async (UsuarioInstituicaoResponseDTO req, IUsuarioInstituicaoService usuarioInstituicaoService) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);
                var resultado = await usuarioInstituicaoService.VincularUsuarioInstituicaoAsync(req);
                return Results.Ok(new
                {
                    message = "Usuário vinculado à instituição com sucesso",
                    usuarioInstituicao = resultado
                });
            }).RequireAuthorization("Gestao");

            route.MapGet("verificar-vinculo", async (Guid usuarioId, Guid instituicaoId, IUsuarioInstituicaoService usuarioInstituicaoService) =>
            {
                var resultado = await usuarioInstituicaoService.VerificarVinculoUsuarioInstituicaoAsync(usuarioId, instituicaoId);
                return Results.Ok(new
                {
                    message = "Verificação de vínculo concluída",
                    vinculado = resultado
                });
            }).RequireAuthorization("Gestao");

            route.MapDelete("desvincular", async (Guid usuarioId, Guid instituicaoId, IUsuarioInstituicaoService usuarioInstituicaoService) =>
            {
                var resultado = await usuarioInstituicaoService.DesvincularUsuarioInstituicaoAsync(usuarioId, instituicaoId);
                if (!resultado)
                {
                    return Results.NotFound(new { message = "Vínculo não encontrado." });
                }
                return Results.Ok(new { message = "Usuário desvinculado da instituição com sucesso." });
            }).RequireAuthorization("Gestao");

            route.MapGet("listar-instituicoes-por-usuario", async (Guid usuarioId, IUsuarioInstituicaoService usuarioInstituicaoService) =>
            {
                var instituicoes = await usuarioInstituicaoService.ListarInstituicoesPorUsuarioAsync(usuarioId);
                return Results.Ok(new
                {
                    message = "Lista de instituições do usuário",
                    instituicoes
                });
            }).RequireAuthorization("Gestao");

            route.MapGet("listar-usuarios-por-instituicao", async (Guid instituicaoId, IUsuarioInstituicaoService usuarioInstituicaoService) =>
            {
                var usuarios = await usuarioInstituicaoService.ListarUsuariosPorInstituicaoAsync(instituicaoId);
                return Results.Ok(new
                {
                    message = "Lista de usuários da instituição",
                    usuarios
                });
            }).RequireAuthorization("Gestao");
        }
    }
}
