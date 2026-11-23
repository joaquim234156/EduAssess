namespace EduAssess.Application.DTOs.Instituicao
{
    public record InstituicaoResponseDTO(
        Guid Id,
        string Nome,
        string Cnpj,
        string Endereco,
        string Telefone,
        string Email,
        bool Ativa,
        string Tipo = "Faculdade",
        string? Sigla = null,
        Guid? MantenedorId = null,
        string? MantenedorNome = null,
        string CorPrimaria = "#1F4E79",
        string CorSecundaria = "#D6A94B",
        bool TemLogo = false
    );

    /// <summary>NOVO: mantenedor (dono) e as instituições que ele mantém (Faculdade, Colégio...).</summary>
    public record MantenedorResponseDTO(Guid Id, string Nome, List<MantenedorInstituicaoDTO> Instituicoes);
    public record MantenedorInstituicaoDTO(Guid Id, string Nome, string Tipo, string? Sigla, bool Ativa);
}
