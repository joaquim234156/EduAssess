using System.ComponentModel.DataAnnotations;

namespace EduAssess.EduAssess.Aplication.DTOs.User
{
    public record PasswordRequest(
        [Required(ErrorMessage = "A Senha é obrigatório")]
        [MinLength(8, ErrorMessage = "A Senha deve ter no mínimo 8 caracteres.")]
        [MaxLength(250, ErrorMessage = "A Senha deve ter no máximo 250 caracteres.")]
        string password,
        [Required(ErrorMessage = "A Senha é obrigatório")]
        [MinLength(8, ErrorMessage = "A Senha deve ter no mínimo 8 caracteres.")]
        [MaxLength(250, ErrorMessage = "A Senha deve ter no máximo 250 caracteres.")]
        string? confirmPassword = null
    );
}
