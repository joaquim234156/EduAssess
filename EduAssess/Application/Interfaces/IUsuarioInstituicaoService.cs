using EduAssess.Application.DTOs.UsuarioInstituicao;
using EduAssess.Models.Global;

namespace EduAssess.Application.Interfaces
{
    public interface IUsuarioInstituicaoService
    {
        Task<UsuarioInstituicaoResponseDTO> VincularUsuarioInstituicaoAsync(UsuarioInstituicaoResponseDTO req);
        Task<bool> DesvincularUsuarioInstituicaoAsync(Guid usuarioId, Guid instituicaoId);
        Task<IEnumerable<Guid>> ListarInstituicoesPorUsuarioAsync(Guid usuarioId);
        Task<IEnumerable<Guid>> ListarUsuariosPorInstituicaoAsync(Guid instituicaoId);
        Task<bool> VerificarVinculoUsuarioInstituicaoAsync(Guid usuarioId, Guid instituicaoId);
    }
}
