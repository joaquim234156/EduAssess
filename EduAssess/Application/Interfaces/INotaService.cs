using EduAssess.Application.DTOs.Nota;

namespace EduAssess.Application.Interfaces
{
    public interface INotaService
    {
        Task<NotaResponseDTO> RegistrarNotaAsync(NotaRequest request);
        Task<IEnumerable<NotaResponseDTO>> ObterNotasPorMatriculaAsync(Guid idMatricula);
        Task<IEnumerable<NotaResponseDTO>> ObterNotasDoAlunoAsync(Guid idAluno);
        Task<IEnumerable<NotaResponseDTO>> ListarTodasAsync();
    }
}
