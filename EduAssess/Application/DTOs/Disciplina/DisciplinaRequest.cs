using System.ComponentModel.DataAnnotations;

namespace EduAssess.Application.DTOs.Disciplina
{
    public record DisciplinaRequest(
        [Required(ErrorMessage = "O Nome da Disciplina é obrigatório!")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O Nome da Disciplina deve ter entre 3 e 100 caracteres.")]
        string nome,
        [Required(ErrorMessage = "A Descrição da Disciplina é obrigatória!")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "A Descrição da Disciplina deve ter entre 10 e 500 caracteres.")]
        string descricao
        );
}