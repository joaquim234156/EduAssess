using EduAssess.Extensions;

namespace EduAssess.Models.Global
{
    public enum TipoUnidade { Campus = 1, Polo = 2, Predio = 3 }
    public class UnidadeModel
    {
        protected UnidadeModel() { }

        public UnidadeModel(Guid instituicaoId, string nome, string sigla, TipoUnidade tipo)
        {
            Id = Guid.NewGuid();
            InstituicaoId = instituicaoId;
            Nome = nome;
            Sigla = sigla;
            Slug = nome.GenerateSlug();
            Tipo = tipo;
            Ativa = true;
            CriadoEm = DateTime.UtcNow;
            AtualizadaEm = DateTime.UtcNow;
        }

        public Guid Id { get; set; }
        public Guid InstituicaoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public TipoUnidade Tipo { get; set; }
        public string Sigla { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public bool Ativa { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime AtualizadaEm { get; set; }
        public InstituicaoModel? Instituicao { get; set; }

        // Identidade visual opcional da unidade. Nulo/vazio significa herdar da instituição.
        public string? LogoPath { get; private set; }
        public string? CorPrimaria { get; private set; }
        public string? CorSecundaria { get; private set; }

        public void Update(string nome, TipoUnidade tipo, string sigla)
        {
            Nome = nome;
            Tipo = tipo;
            Sigla = sigla;
            Slug = nome.GenerateSlug();
            AtualizadaEm = DateTime.UtcNow;
        }

        public void UpdateBranding(string? logoPath, string? corPrimaria, string? corSecundaria)
        {
            if (corPrimaria is not null && !InstituicaoModel.IsHexColor(corPrimaria))
                throw new ArgumentException("A cor primária deve estar no formato #RRGGBB.");
            if (corSecundaria is not null && !InstituicaoModel.IsHexColor(corSecundaria))
                throw new ArgumentException("A cor secundária deve estar no formato #RRGGBB.");

            LogoPath = string.IsNullOrWhiteSpace(logoPath) ? null : logoPath;
            CorPrimaria = string.IsNullOrWhiteSpace(corPrimaria) ? null : corPrimaria.ToUpperInvariant();
            CorSecundaria = string.IsNullOrWhiteSpace(corSecundaria) ? null : corSecundaria.ToUpperInvariant();
            AtualizadaEm = DateTime.UtcNow;
        }

        public void Disable()
        {
            Ativa = false;
            AtualizadaEm = DateTime.UtcNow;
        }

        public void Enable()
        {
            Ativa = true;
            AtualizadaEm = DateTime.UtcNow;
        }

    }
}
