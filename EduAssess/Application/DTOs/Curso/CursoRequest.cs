using System.ComponentModel.DataAnnotations;

namespace EduAssess.Application.DTOs.Curso
{
    public record CursoRequest(
        [Required(ErrorMessage = "O nome do curso é obrigatório.")]
        [StringLength(60, MinimumLength = 3, ErrorMessage = "O nome do curso deve ter entre 3 e 60 caracteres.")]
        string nome,
        [Range(1, 20, ErrorMessage = "A duração do curso deve ser de 1 a 20 anos.")]
        int duracaoEmAnos,
        [MaxLength(500, ErrorMessage = "A descrição do curso não pode ultrapassar 500 caracteres.")]
        string? descricao = null
    );
}
