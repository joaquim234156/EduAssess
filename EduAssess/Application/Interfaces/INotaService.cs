using EduAssess.Application.DTOs.Nota;

namespace EduAssess.Application.Interfaces
{
    public interface INotaService
    {
        Task<NotaResponseDTO> RegistrarNotaAsync(NotaRequest request, Guid professorId);
        Task<IEnumerable<NotaResponseDTO>> ObterNotasPorMatriculaAsync(Guid idMatricula);
        Task<IEnumerable<NotaResponseDTO>> ListarTodasAsync();
    }
}
