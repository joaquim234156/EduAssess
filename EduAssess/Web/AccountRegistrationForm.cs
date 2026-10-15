using System.ComponentModel.DataAnnotations;

namespace EduAssess.Web;

public sealed class AccountRegistrationForm
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Use entre 3 e 100 caracteres.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Informe o CPF.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "Use os 11 números do CPF.")]
    public string Cpf { get; set; } = "";

    [Required(ErrorMessage = "Informe o telefone.")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Use 10 ou 11 números.")]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = "Informe a data de nascimento.")]
    public DateOnly BirthDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(-18));

    [Required(ErrorMessage = "Defina uma senha.")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "Use uma senha entre 8 e 72 caracteres.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirme a senha.")]
    [Compare(nameof(Password), ErrorMessage = "As senhas não coincidem.")]
    public string ConfirmPassword { get; set; } = "";
}
