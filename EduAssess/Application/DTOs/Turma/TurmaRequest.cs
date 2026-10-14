using System.ComponentModel.DataAnnotations;

namespace EduAssess.Application.DTOs.Turma
{
    public record TurmaRequest(
        [Required(ErrorMessage = "O Nome da Turma é obrigatório.")]
        [MaxLength(150, ErrorMessage = "O Nome da Turma deve ter no máximo 150 caracteres.")]
        string nome,
        [Required(ErrorMessage ="È preciso informar um professor")]
        Guid professorId,
        [Required(ErrorMessage = "Selecione o curso da turma para continuar")]
        Guid cursoId
        );
}
