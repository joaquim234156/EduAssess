namespace EduAssess.Application.DTOs.Unidade
{
    public record UnidadeRequest
        (
            string Nome,
            string Cnpj,
            string Endereco,
            string Telefone,
            string Email
        );
}
