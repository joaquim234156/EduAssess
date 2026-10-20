using EduAssess.Application.DTOs.Nota;

namespace EduAssess.Application.Interfaces
{
    public interface INotaService
    {
        // O professorId deve ser o ID já validado a partir do token de autenticação.
        Task<NotaResponseDTO> RegistrarNotaAsync(NotaRequest request, Guid professorId);
        Task<IEnumerable<NotaResponseDTO>> ObterNotasPorMatriculaAsync(Guid idMatricula);
        Task<IEnumerable<NotaResponseDTO>> ListarTodasAsync();
    }
}
