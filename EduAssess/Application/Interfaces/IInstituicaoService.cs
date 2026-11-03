using EduAssess.Application.DTOs.Instituicao;

namespace EduAssess.Application.Interfaces
{
    public interface IInstituicaoService
    {
        Task<InstituicaoResponseDTO> CriarInstituicaoAsync(InstituicaoRequest req);
        Task<InstituicaoResponseDTO> AtualizarInstituicaoAsync(Guid id, InstituicaoRequestPut req);
        Task<InstituicaoResponseDTO> ObterInstituicaoPorIdAsync(Guid id);
        Task<IEnumerable<InstituicaoResponseDTO>> ListarInstituicoesAsync();
        Task<IEnumerable<InstituicaoResponseDTO>> ListarInstituicoesAtivasAsync();
        Task<IEnumerable<InstituicaoResponseDTO>> ListarInstituicoesInativasAsync();
        Task<IEnumerable<InstituicaoResponseDTO>> ListarInstituicoesPorUsuarioAsync(Guid usuarioId);
        Task<bool> DeletarInstituicaoAsync(Guid id);
        Task<bool> AtivarInstituicaoAsync(Guid id);
    }
}
