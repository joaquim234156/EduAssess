namespace EduAssess.Application.DTOs.Instituicao
{
    /// <summary>Campos depois de Email são opcionais: nulo = manter o valor atual.</summary>
    public record InstituicaoRequestPut
        (
        string Nome,
        string Cnpj,
        string Endereco,
        string Telefone,
        string Email,
        string? Tipo = null,
        string? Sigla = null,
        Guid? MantenedorId = null,
        string? MantenedorNome = null,
        string? CorPrimaria = null,
        string? CorSecundaria = null
        );

    /// <summary>NOVO: só a identidade visual (PUT /instituicoes/{id}/identidade).</summary>
    public record InstituicaoIdentidadeRequest(string CorPrimaria, string CorSecundaria);
}
