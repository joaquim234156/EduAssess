using EduAssess.Extensions;

namespace EduAssess.Models.Global
{
    /// <summary>
    /// NOVO: categoria da instituição. O Colégio Luciano Feijão (CLF) NÃO é uma faculdade: é um braço
    /// institucional próprio (<see cref="Colegio"/>), gerenciado separadamente, mas ligado ao mesmo
    /// <see cref="MantenedorModel"/> (dono/mantenedor) da Faculdade Luciano Feijão.
    /// </summary>
    public enum TipoInstituicao { Faculdade = 1, Colegio = 2, Escola = 3, Instituto = 4, CentroTecnico = 5 }

    public class InstituicaoModel
    {
        protected InstituicaoModel() { }

        public InstituicaoModel(string nome, string cnpj, string email, string telefone, string endereco,
            TipoInstituicao tipo = TipoInstituicao.Faculdade, string? sigla = null, Guid? mantenedorId = null)
        {
            Tipo = tipo;
            Sigla = string.IsNullOrWhiteSpace(sigla) ? null : sigla.Trim().ToUpperInvariant();
            MantenedorId = mantenedorId;
            Id = Guid.NewGuid();
            Nome = nome;
            Cnpj = cnpj;
            Email = email;
            Telefone = telefone;
            Endereco = endereco;
            Slug = nome.GenerateSlug();
            Ativa = true;
            CriadaEm = DateTime.UtcNow;
            AtualizadaEm = DateTime.UtcNow;
        }

        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cnpj { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Endereco { get; set; } = string.Empty;
        public bool Ativa { get; set; }
        public string Slug { get; set; } = string.Empty;
        public DateTime CriadaEm { get; set; }
        public DateTime AtualizadaEm { get; set; }
        public Guid? MantenedorId { get; set; }
        public MantenedorModel? Mantenedor { get; set; }
        public string? LogoPath { get; private set; }
        public string CorPrimaria { get; private set; } = "#1F4E79";
        public string CorSecundaria { get; private set; } = "#D6A94B";
        /// <summary>NOVO: Faculdade ou Colégio (CLF). Guardado como texto no banco.</summary>
        public TipoInstituicao Tipo { get; private set; } = TipoInstituicao.Faculdade;
        /// <summary>NOVO: sigla curta (FLF, CLF) usada em selos e no seletor de instituição.</summary>
        public string? Sigla { get; private set; }
        public List<UnidadeModel> Unidades { get; set; } = new();

        /// <summary>NOVO: valida #RRGGBB (nunca aceitamos CSS livre: a cor vai para um atributo style).</summary>
        public static bool IsHexColor(string? value) =>
            !string.IsNullOrEmpty(value) && value.Length == 7 && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);

        /// <summary>NOVO: muda categoria, sigla e mantenedor. O Colégio exige mantenedor (é o vínculo com o dono).</summary>
        public void UpdateClassificacao(TipoInstituicao tipo, string? sigla, Guid? mantenedorId)
        {
            if (!Enum.IsDefined(tipo)) throw new ArgumentException("Tipo de instituição inválido.");
            if (tipo == TipoInstituicao.Colegio && mantenedorId is null)
                throw new ArgumentException("O colégio precisa estar vinculado a um mantenedor.");
            Tipo = tipo;
            Sigla = string.IsNullOrWhiteSpace(sigla) ? null : sigla.Trim().ToUpperInvariant();
            MantenedorId = mantenedorId;
            AtualizadaEm = DateTime.UtcNow;
        }

        public void Update(string nome, string cnpj, string email, string telefone, string endereco)
        {
            Nome = nome;
            Cnpj = cnpj;
            Email = email;
            Telefone = telefone;
            Endereco = endereco;
            Slug = nome.GenerateSlug();
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

        public void UpdateBranding(string? logoPath, string corPrimaria, string corSecundaria)
        {
            if (!IsHexColor(corPrimaria) || !IsHexColor(corSecundaria))
                throw new ArgumentException("As cores devem estar no formato #RRGGBB.");
            LogoPath = logoPath;
            CorPrimaria = corPrimaria.ToUpperInvariant();
            CorSecundaria = corSecundaria.ToUpperInvariant();
            AtualizadaEm = DateTime.UtcNow;
        }
    }
}
