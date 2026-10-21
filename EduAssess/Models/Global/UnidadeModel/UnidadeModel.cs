namespace EduAssess.Models.Global
{
    public enum TipoUnidade { Campus = 1, Polo = 2 }
    public class UnidadeModel
    {
        protected UnidadeModel() { }

        public UnidadeModel(Guid instituicaoId, string nome, string codigo, TipoUnidade tipo, string sigla)
        {
            Id = Guid.NewGuid();
            InstituicaoId = instituicaoId;
            Nome = nome;
            Codigo = codigo;
            Tipo = tipo;
            Ativa = true;
            CriadoEm = DateTime.UtcNow;
            AtualizadaEm = DateTime.UtcNow;
        }

        public Guid Id { get; set; }
        public Guid InstituicaoId { get; set; }
        public string Nome { get; set; } = null;
        public string Codigo { get; set; }
        public TipoUnidade Tipo { get; set; }
        public bool Ativa { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime AtualizadaEm { get; set; }
        public InstituicaoModel? Instituicao { get; set; }

    }
}
