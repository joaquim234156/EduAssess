using System.ComponentModel.DataAnnotations;

namespace EduAssess.Models.UserModel
{
    public record UserRequest(
        [Required(ErrorMessage = "O Nome é obrigatório.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 120 caracteres.")]
        string name,
        [Required(ErrorMessage = "O Email é obrigatório.")]
        [MaxLength(150, ErrorMessage = "O Email deve ter no máximo 150 caracteres.")]
        [EmailAddress(ErrorMessage = "O Email informado não é válido.")]
        string email,
        [Required(ErrorMessage = "A Senha é obrigatório")]
        [MinLength(8, ErrorMessage = "A Senha deve ter no mínimo 8 caracteres.")]
        [MaxLength(250, ErrorMessage = "A Senha deve ter no máximo 250 caracteres.")]
        string password
    );
}
