using System.ComponentModel.DataAnnotations;

namespace EduAssess.Models.UserModel
{
    public record UserRequestPut(
        [Required(ErrorMessage = "O Email é obrigatório.")]
        [MaxLength(150, ErrorMessage = "O Email deve ter no máximo 150 caracteres.")]
        [EmailAddress(ErrorMessage = "O Email informado não é válido.")]
        string email,

        UserRole role,

        [MinLength(8, ErrorMessage = "A Senha deve ter no mínimo 8 caracteres.")]
        string? password = null

    );
}
