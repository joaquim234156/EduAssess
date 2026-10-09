using EduAssess.Models;
using System.ComponentModel.DataAnnotations;

namespace EduAssess.EduAssess.Aplication.DTOs.User
{
    public record UserRequestPutDetails(
        [Required(ErrorMessage = "O Nome é obrigatório.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 120 caracteres.")]
        string name,
        [Required(ErrorMessage = "O Email é obrigatório.")]
        [MaxLength(150, ErrorMessage = "O Email deve ter no máximo 150 caracteres.")]
        [EmailAddress(ErrorMessage = "O Email informado não é válido.")]
        string email,
        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "O CPF deve conter exatamente 11 dígitos numéricos.")]
        string cpf,
        [Required(ErrorMessage = "O Telefone/WhatsApp é obrigatório.")]
        [RegularExpression(@"^\d{10,11}$", ErrorMessage = "O telefone deve conter entre 10 e 11 dígitos numéricos (DDD + Número).")]
        string tel,
        [Required(ErrorMessage = "A data de nascimento é obrigatório.")]
        DateOnly dataNascimento,

        UserRole role = UserRole.User

    );
}
