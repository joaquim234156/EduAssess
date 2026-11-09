using EduAssess.Application.DTOs.UsuarioInstituicao;

namespace EduAssess.Application.Interfaces
{
    public interface IUsuarioInstituicaoService
    {
        // ALTERADO: recebia o ResponseDTO como entrada (confuso, e o Request nunca era usado).
        // Agora recebe o Request, valida tudo e reaproveita um vínculo antigo em vez de duplicar a chave.
        Task<UsuarioInstituicaoResponseDTO> VincularUsuarioInstituicaoAsync(UsuarioInstituicaoRequest req);

        // NOVO: muda o papel e/ou a unidade (prédio) de um vínculo existente.
        Task<UsuarioInstituicaoResponseDTO> AtualizarUsuarioInstituicaoAsync(UsuarioInstituicaoRequestPut req);

        Task<bool> DesvincularUsuarioInstituicaoAsync(Guid usuarioId, Guid instituicaoId);
        Task<IEnumerable<Guid>> ListarInstituicoesPorUsuarioAsync(Guid usuarioId);
        Task<IEnumerable<Guid>> ListarUsuariosPorInstituicaoAsync(Guid instituicaoId);
        Task<bool> VerificarVinculoUsuarioInstituicaoAsync(Guid usuarioId, Guid instituicaoId);
    }
}
