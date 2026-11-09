namespace EduAssess.Application.DTOs.User
{
    public record UserResponseDTO
        (Guid Id,
        string Nome,
        string Email,
        string CPF,
        string Telefone,
        DateOnly DataNascimento,
        bool Ativa);
}
