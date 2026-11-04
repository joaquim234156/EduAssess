namespace EduAssess.Application.DTOs.Instituicao
{
    public record InstituicaoRequestPut
        (
        string Nome,
        string Cnpj,
        string Endereco,
        string Telefone,
        string Email
        );
}
