using EduAssess.Application.DTOs.Curso;

namespace EduAssess.Application.Interfaces
{
    public interface ICursoService
    {
        Task<CursoResponseDTO> CriarCursoAsync(CursoRequest req);
        Task<CursoResponseDTO> ObterCursoPorIdAsync(Guid id);
        Task<CursoResponseDTO> AtualizarCursoAsync(Guid id, CursoRequest req);
        Task<IEnumerable<CursoResponseDTO>> ListarCursosAsync();
        Task<IEnumerable<CursoResponseDTO>> ListarCursosAtivosAsync();
        Task<IEnumerable<CursoResponseDTO>> ListarCursosInativosAsync();
        Task<bool> DeletarCursoAsync(Guid id);
        Task<bool> AtivarCursoAsync(Guid id);
    }
}
