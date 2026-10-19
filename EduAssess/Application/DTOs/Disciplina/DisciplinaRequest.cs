using System.ComponentModel.DataAnnotations;

namespace EduAssess.Application.DTOs.Disciplina
{
    public record DisciplinaRequest(
        [Required(ErrorMessage = "O Nome da Disciplina é obrigatório!")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O Nome da Disciplina deve ter entre 3 e 100 caracteres.")]
        string nome,
        string descricao,
        Guid professorId
        );
}