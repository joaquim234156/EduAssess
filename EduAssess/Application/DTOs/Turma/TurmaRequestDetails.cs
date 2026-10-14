using System.ComponentModel.DataAnnotations;

namespace EduAssess.Application.DTOs.Turma
{
    public record TurmaRequestDetails(
        [Required(ErrorMessage = "O Nome da Turma é obrigatório.")]
        [MaxLength(150, ErrorMessage = "O Nome da Turma deve ter no máximo 150 caracteres.")]
        string nome,
        [Required(ErrorMessage = "É obrigatório o nome do professor responsável.")]
        [MaxLength(150, ErrorMessage = "O Nome do Professor deve ter no máximo 150 caracteres.")]
        [MinLength(3, ErrorMessage = "O Nome do Professor deve ter no mínimo 3 caracteres.")]
        [RegularExpression(@"^[a-zA-ZÀ-ÿ\s]+$", ErrorMessage = "O Nome do Professor deve conter apenas letras e espaços.")]
        Guid professorId,
        [Required(ErrorMessage = "Selecione o curso da turma para continuar")]
        Guid cursoId
        );
}
