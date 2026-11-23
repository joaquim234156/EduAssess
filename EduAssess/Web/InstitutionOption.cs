using EduAssess.Models.Global;

namespace EduAssess.Web;

public sealed record InstitutionOption(Guid Id, string Name, TipoInstituicao Tipo = TipoInstituicao.Faculdade, string? Sigla = null);

/// <summary>NOVO: identidade visual da instituição em uso (logo e cores), disponível para o layout do painel.</summary>
public sealed record InstitutionBrand(Guid Id, string Name, string? Sigla, TipoInstituicao Tipo, bool HasLogo, long LogoVersion, string Primary, string Secondary, string? CustomLogoUrl = null)
{
    /// <summary>URL do arquivo de marca efetivo: logotipo da unidade, quando existe, ou da instituição.</summary>
    public string ImageUrl => CustomLogoUrl ?? $"/ui/instituicao-logo/{Id}?v={LogoVersion}";
    /// <summary>Cor de texto legível sobre a cor primária (branco ou quase-preto), pela luminância.</summary>
    public string PrimaryInk => Ink(Primary);
    public string SecondaryInk => Ink(Secondary);

    public string Style => $"--brand-primary:{Primary};--brand-secondary:{Secondary};--brand-primary-ink:{PrimaryInk};--brand-secondary-ink:{SecondaryInk}";

    private static string Ink(string hex)
    {
        if (hex.Length != 7) return "#fff";
        var r = Convert.ToInt32(hex.Substring(1, 2), 16);
        var g = Convert.ToInt32(hex.Substring(3, 2), 16);
        var b = Convert.ToInt32(hex.Substring(5, 2), 16);
        return (0.299 * r + 0.587 * g + 0.114 * b) > 160 ? "#0c1d45" : "#ffffff";
    }
}
