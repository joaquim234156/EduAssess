using EduAssess.Extensions;

namespace EduAssess.Models.Global
{
    public enum TipoUnidade { Campus = 1, Polo = 2 }
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

        public void Update(string nome, TipoUnidade tipo, string sigla)
        {
            Nome = nome;
            Tipo = tipo;
            Sigla = sigla;
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

    }
}
