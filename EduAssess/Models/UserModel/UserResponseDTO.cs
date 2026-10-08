namespace EduAssess.Models.UserModel
{
    public record UserResponseDTO
        (Guid Id,
        string Nome,
        string Email,
        string CPF,
        string Telefone,
        DateTime DataNascimento,
        bool Ativa);
}
