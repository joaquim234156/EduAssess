using System.ComponentModel.DataAnnotations;

namespace EduAssess.Application.DTOs.Curso
{
    public record CursosRequest   (
        [Required(ErrorMessage = "O Nome do Curso é obrigatório!")]
        [MaxLength(60, ErrorMessage = "O Nome do Curso não pode ultrapassar os 60 caracteres!!!")]
        [MinLength(3, ErrorMessage = "O Nome do Curso deve ter no Mínimo 3 caracteres para prosseguir!!!")]
        string nome,
        [Required(ErrorMessage = "O Nome do Professor é obrigatório!")]
        [MaxLength(60, ErrorMessage = "O Nome do Professor não pode ultrapassar os 60 caracteres!!!")]
        [MinLength(3, ErrorMessage = "O Nome do Professor deve ter no Mínimo 3 caracteres para prosseguir!!!")]
        [RegularExpression(@"^[a-zA-ZÀ-ÿ\s]+$",ErrorMessage = "O Nome do Professor deve conter apenas letras e espaços!")]
        string professor,
        [Required(ErrorMessage = "A Descrição do Curso é obrigatória!")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "A Descrição do Curso deve conter apenas números!")]
        int duracaoEmAnos,
        [MaxLength(500, ErrorMessage = "A Descrição do Curso não pode ultrapassar os 500 caracteres!!!")]
        string descricao
    );
}
