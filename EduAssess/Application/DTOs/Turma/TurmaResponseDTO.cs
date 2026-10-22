namespace EduAssess.Application.DTOs.Turma
{
    public record TurmaResponseDTO
    (
        Guid IdTurma,
        string NomeTurma,
        Guid IdCurso,
        Guid IdProfessor,
        int Periodo,
        bool Ativo
    );
}
