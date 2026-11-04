namespace EduAssess.Application.DTOs.Instituicao
{
    public record InstituicaoResponseDTO(
        Guid Id,
        string Nome,
        string Cnpj,
        string Endereco,
        string Telefone,
        string Email,
        bool Ativa
    );
}
