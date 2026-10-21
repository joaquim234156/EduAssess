using EduAssess.Application.DTOs.Disciplina;

namespace EduAssess.Application.Interfaces
{
    public interface IDisciplinaService
    {
        Task<DisciplinaResponseDTO> CriarDisciplinaAsync(DisciplinaRequest req, Guid professorId);
        Task<DisciplinaResponseDTO> ObterDisciplinaPorIdAsync(Guid id);
        Task<DisciplinaResponseDTO> AtualizarDisciplinaAsync(Guid id, DisciplinaRequestPut req);
        Task<IEnumerable<DisciplinaResponseDTO>> ListarDisciplinasAsync();
        Task<bool> DeletarDisciplinaAsync(Guid id);
    }
}
