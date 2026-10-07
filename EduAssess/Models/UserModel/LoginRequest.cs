using System.ComponentModel.DataAnnotations;

namespace EduAssess.Models.UserModel
{
    public record LoginRequest(
        [Required(ErrorMessage = "O Email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O Email informado não é válido.")]
        string email,

        [Required(ErrorMessage = "A Senha é obrigatória.")]
        string password
    );
}
