namespace EduAssess.Application.DTOs.Matricula
{
    public record MatriculaResponseDTO
    (
        Guid id,
        Guid usuarioId,
        Guid turmaId,
        DateTime dataMatricula
    );
}
