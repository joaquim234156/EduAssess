using EduAssess.Application.DTOs.Matricula;

namespace EduAssess.Application.Interfaces
{
    public interface IMatriculaService
    {
        Task<MatriculaResponseDTO> CriarMatriculaAsync(MatriculaRequest req);
        Task<MatriculaResponseDTO> ObterMatriculaPorIdAsync(Guid id);
        Task<MatriculaResponseDTO> AtualizarMatriculaAsync(Guid id, MatriculaRequestPut req);
        Task<IEnumerable<MatriculaResponseDTO>> ListarMatriculasAsync();
        Task<bool> DeletarMatriculaAsync(Guid id);  
    }
}
