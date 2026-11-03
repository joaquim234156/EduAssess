namespace EduAssess.Application.DTOs.Unidade
{
    public record UnidadeRequestPut(
        string Nome,
        string Cnpj,
        string Endereco,
        string Telefone,
        string Email);
}
