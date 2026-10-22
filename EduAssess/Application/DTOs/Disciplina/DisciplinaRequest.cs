using System.ComponentModel.DataAnnotations;

namespace EduAssess.Application.DTOs.Disciplina
{
    public record DisciplinaRequest(
        [Required(ErrorMessage = "O nome da disciplina é obrigatório.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome da disciplina deve ter entre 3 e 100 caracteres.")]
        string nome,
        [MaxLength(500, ErrorMessage = "A descrição não pode ultrapassar 500 caracteres.")]
        string? descricao,
        Guid professorId
        );
}
