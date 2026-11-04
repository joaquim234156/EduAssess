namespace EduAssess.Application.DTOs.Instituicao
{
    public record InstituicaoRequest(
        string Nome,
        string Cnpj,
        string Endereco,
        string Telefone,
        string Email
    );
}
