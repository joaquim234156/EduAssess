using System.ComponentModel.DataAnnotations;

namespace EduAssess.Web;

public sealed class AccountRegistrationForm
{
    private string cpf = "";
    private string phone = "";

    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Use entre 3 e 100 caracteres.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
    public string Email { get; set; } = "";

    /// <summary>Aceita máscara (123.456.789-01); guarda apenas os dígitos.</summary>
    [Required(ErrorMessage = "Informe o CPF.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "Use os 11 números do CPF.")]
    public string Cpf
    {
        get => cpf;
        set => cpf = UiFormat.Digits(value);
    }

    /// <summary>Aceita máscara ((11) 98765-4321); guarda apenas os dígitos.</summary>
    [Required(ErrorMessage = "Informe o telefone.")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Use 10 ou 11 números (DDD + número).")]
    public string Phone
    {
        get => phone;
        set => phone = UiFormat.Digits(value);
    }

    [Required(ErrorMessage = "Informe a data de nascimento.")]
    public DateOnly BirthDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(-18));

    [Required(ErrorMessage = "Defina uma senha.")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "Use uma senha entre 8 e 72 caracteres.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirme a senha.")]
    [Compare(nameof(Password), ErrorMessage = "As senhas não coincidem.")]
    public string ConfirmPassword { get; set; } = "";
}
