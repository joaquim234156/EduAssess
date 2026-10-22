using EduAssess.Application.DTOs.Turma;

namespace EduAssess.Application.Interfaces
{
    public interface ITurmaService
    {
        Task<TurmaResponseDTO> CriarTurmaAsync(TurmaRequest req);
        Task<TurmaResponseDTO> ObterTurmaPorIdAsync(Guid id);
        Task<TurmaResponseDTO> AtualizarTurmaAsync(Guid id, TurmaRequestPut req);
        Task<IEnumerable<TurmaResponseDTO>> ListarTurmasAsync();
        Task<IEnumerable<TurmaResponseDTO>> ListarTurmasPorProfessorAsync(Guid professorId);
        Task<bool> DeletarTurmaAsync(Guid id);
        Task<bool> AtivarTurmaAsync(Guid id);
    }
}
