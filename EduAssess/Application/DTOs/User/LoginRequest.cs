using System.ComponentModel.DataAnnotations;

namespace EduAssess.EduAssess.Aplication.DTOs.User
{
    public record LoginRequest(
        [Required(ErrorMessage = "O Email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O Email informado não é válido.")]
        [StringLength(150, ErrorMessage = "O Email deve ter no máximo 150 caracteres.")]
        string email,

        [Required(ErrorMessage = "A Senha é obrigatória.")]
        [StringLength(72, MinimumLength = 1, ErrorMessage = "A Senha deve ter no máximo 72 caracteres.")]
        string password
    );
}
