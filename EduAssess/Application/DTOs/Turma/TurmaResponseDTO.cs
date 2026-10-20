using System.ComponentModel.DataAnnotations;

namespace EduAssess.Application.DTOs.Turma
{
    public record TurmaResponseDTO(
        Guid Id,
        string Nome,
        Guid ProfessorId,
        Guid CursoId,
        int Periodo
        );
}
