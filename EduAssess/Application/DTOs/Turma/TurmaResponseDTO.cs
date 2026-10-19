namespace EduAssess.Application.DTOs.Turma
{
    public record TurmaResponseDTO
    (
        Guid IdTurma,
        string NomeTurma,
        Guid IdCurso,
        DateTime DataInicio,
        DateTime DataFim
    );
}
