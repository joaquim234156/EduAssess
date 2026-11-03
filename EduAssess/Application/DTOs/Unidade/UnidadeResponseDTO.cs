namespace EduAssess.Application.DTOs.Unidade
{
    public record UnidadeResponseDTO(
        int Id,
        string Nome,
        string Cnpj,
        string Endereco,
        string Telefone,
        string Email
    );
}
