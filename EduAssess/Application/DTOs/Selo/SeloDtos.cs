namespace EduAssess.Application.DTOs.Selo
{
    /// <summary>Selo escolhido para uma questão (entrada). Codigo = código do tipo (IA, ENEM...).</summary>
    public sealed record SeloInput(string Codigo, int? Ano = null, string? Detalhe = null, bool Exibir = true);

    /// <summary>Selo de uma questão (saída).</summary>
    public sealed record SeloDto(string Codigo, string Nome, string Cor, int? Ano, string? Detalhe, bool Exibir, string Texto);

    public sealed record SeloTipoDto(Guid Id, string Codigo, string Nome, string? Descricao, string Cor, bool PermiteAno, bool ExigeAno,
        bool PermiteDetalhe, bool ExigeDetalhe, string? RotuloDetalhe);

    public sealed record SeloTipoRequest(string Codigo, string Nome, string? Cor = null, string? Descricao = null, bool PermiteAno = false,
        bool ExigeAno = false, bool PermiteDetalhe = false, bool ExigeDetalhe = false, string? RotuloDetalhe = null);
}
