using EduAssess.Application.DTOs.Unidade;
using EduAssess.Models.Global;

namespace EduAssess.Application.Interfaces
{
    public interface IUnidadeService
    {
        Task<UnidadeResponseDTO> CriarUnidadeAsync(UnidadeRequest req);
        Task<UnidadeResponseDTO> ObterUnidadePorIdAsync(Guid id);
        Task<UnidadeResponseDTO> AtualizarUnidadeAsync(Guid id, UnidadeRequestPut req);
        Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesAsync();
        Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesAtivasAsync();
        Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesInativasAsync();
        Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesPorInstituicaoAsync(Guid instituicaoId);
        Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesPorTipoAsync(TipoUnidade tipo);
        Task<IEnumerable<UnidadeResponseDTO>> ListarUnidadesPorInstituicaoETipoAsync(Guid instituicaoId, TipoUnidade tipo);
        Task<bool> AtivarUnidadeAsync(Guid id);
        Task<bool> DeletarUnidadeAsync(Guid id);
    }
}
