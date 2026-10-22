namespace EduAssess.Application.DTOs.Matricula
{
    public record MatriculaResponseDTO
    (
        Guid Id,
        Guid UsuarioId,
        Guid TurmaId,
        DateTime DataMatricula,
        bool Ativa
    );
}
