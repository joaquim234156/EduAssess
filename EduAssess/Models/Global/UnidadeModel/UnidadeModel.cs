namespace EduAssess.Models.Global
{
    public class UnidadeModel
    {
        protected UnidadeModel() { }

        public UnidadeModel(Guid instituicaoId, string nome, string sigla)
        {
            Id = Guid.NewGuid();
            InstituicaoId = instituicaoId;
            Nome = nome;
            Ativa = true;
            CriadoEm = DateTime.UtcNow;
            AtualizadaEm = DateTime.UtcNow;
        }

        public Guid Id { get; set; }
        public Guid InstituicaoId { get; set; }
        public string Nome { get; set; } = null;
        public bool Ativa { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime AtualizadaEm { get; set; }

    }
}
