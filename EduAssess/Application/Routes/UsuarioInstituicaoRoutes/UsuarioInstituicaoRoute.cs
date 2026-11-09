using EduAssess.Application.DTOs.UsuarioInstituicao;
using EduAssess.Application.Interfaces;

namespace EduAssess.Application.Routes.UsuarioInstituicaoRoutes
{
    public static class UsuarioInstituicaoRoute
    {
        public static void UsuarioInstituicaoRoutes(this WebApplication app)
        {
            var route = app.MapGroup("/usuario-instituicao").WithTags("UsuarioInstituicao");

            // ALTERADO: o corpo agora é o UsuarioInstituicaoRequest (antes era o ResponseDTO) e inclui a unidade.
            // Erros de regra viram respostas claras (404/400/409) em vez de 500.
            route.MapPost("vincular", async (UsuarioInstituicaoRequest req, IUsuarioInstituicaoService service) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);

                try
                {
                    var resultado = await service.VincularUsuarioInstituicaoAsync(req);
                    return Results.Ok(new
                    {
                        message = "Usuário vinculado à instituição com sucesso",
                        usuarioInstituicao = resultado
                    });
                }
                catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
                catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
                catch (InvalidOperationException ex) { return Results.Conflict(new { message = ex.Message }); }
            }).RequireAuthorization("Gestao");

            // NOVO: muda papel e/ou unidade (prédio) de quem já está vinculado.
            route.MapPut("atualizar", async (UsuarioInstituicaoRequestPut req, IUsuarioInstituicaoService service) =>
            {
                if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                    return Results.BadRequest(errors);

                try
                {
                    var resultado = await service.AtualizarUsuarioInstituicaoAsync(req);
                    return Results.Ok(new { message = "Vínculo atualizado com sucesso", usuarioInstituicao = resultado });
                }
                catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
                catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
            }).RequireAuthorization("Gestao");

            route.MapGet("verificar-vinculo", async (Guid usuarioId, Guid instituicaoId, IUsuarioInstituicaoService service) =>
            {
                var resultado = await service.VerificarVinculoUsuarioInstituicaoAsync(usuarioId, instituicaoId);
                return Results.Ok(new
                {
                    message = "Verificação de vínculo concluída",
                    vinculado = resultado
                });
            }).RequireAuthorization("Gestao");

            route.MapDelete("desvincular", async (Guid usuarioId, Guid instituicaoId, IUsuarioInstituicaoService service) =>
            {
                var resultado = await service.DesvincularUsuarioInstituicaoAsync(usuarioId, instituicaoId);
                if (!resultado)
                {
                    return Results.NotFound(new { message = "Vínculo não encontrado." });
                }
                return Results.Ok(new { message = "Usuário desvinculado da instituição com sucesso." });
            }).RequireAuthorization("Gestao");

            route.MapGet("listar-instituicoes-por-usuario", async (Guid usuarioId, IUsuarioInstituicaoService service) =>
            {
                var instituicoes = await service.ListarInstituicoesPorUsuarioAsync(usuarioId);
                return Results.Ok(new
                {
                    message = "Lista de instituições do usuário",
                    instituicoes
                });
            }).RequireAuthorization("Gestao");

            route.MapGet("listar-usuarios-por-instituicao", async (Guid instituicaoId, IUsuarioInstituicaoService service) =>
            {
                var usuarios = await service.ListarUsuariosPorInstituicaoAsync(instituicaoId);
                return Results.Ok(new
                {
                    message = "Lista de usuários da instituição",
                    usuarios
                });
            }).RequireAuthorization("Gestao");
        }
    }
}
