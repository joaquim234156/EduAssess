using System.ComponentModel.DataAnnotations;

namespace EduAssess.Application.DTOs.Turma
{
    public record TurmaRequestPut(
        [Required(ErrorMessage = "O Nome da Turma é obrigatório.")]
        [MaxLength(150, ErrorMessage = "O Nome da Turma deve ter no máximo 150 caracteres.")]
        string nome,
        [Required(ErrorMessage ="É preciso informar um professor")]
        Guid professorId,
        [Required(ErrorMessage = "Selecione o curso da turma para continuar")]
        Guid cursoId,
        [Required(ErrorMessage = "Informe o período da turma")]
        [Range(1, int.MaxValue, ErrorMessage = "O período da turma deve ser um valor positivo")]
        int periodo
        );
}
