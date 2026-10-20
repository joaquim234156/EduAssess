using System.ComponentModel.DataAnnotations;

namespace EduAssess.Application.DTOs.Curso
{
    public record CursoRequest   (
        [Required(ErrorMessage = "O Nome do Curso é obrigatório!")]
        [MaxLength(60, ErrorMessage = "O Nome do Curso não pode ultrapassar os 60 caracteres!!!")]
        [MinLength(3, ErrorMessage = "O Nome do Curso deve ter no Mínimo 3 caracteres para prosseguir!!!")]
        string nome,
        [Required(ErrorMessage = "A Descrição do Curso é obrigatória!")]
        [StringLength(500, ErrorMessage = "A Descrição do Curso não pode ultrapassar os 500 caracteres!!!")]
        string descricao,
        [Required(ErrorMessage = "A Duração do Curso é obrigatória!")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "A Duração do Curso deve conter apenas números!")]
        int duracaoEmAnos
    );
}
