using EduAssess.Extensions;

namespace EduAssess.Models.Global
{
    public enum TipoUnidade { Campus = 1, Polo = 2 }
    public class UnidadeModel
    {
        protected UnidadeModel() { }

        public UnidadeModel(Guid instituicaoId, string nome, string codigo, string slug, TipoUnidade tipo, string sigla)
        {
            Id = Guid.NewGuid();
            InstituicaoId = instituicaoId;
            Nome = nome;
            Codigo = codigo;
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
        public string Codigo { get; set; }
        public string Slug { get; set; } = string.Empty;
        public bool Ativa { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime AtualizadaEm { get; set; }
        public InstituicaoModel? Instituicao { get; set; }

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
