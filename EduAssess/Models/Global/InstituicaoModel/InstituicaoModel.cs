namespace EduAssess.Models.Global
{
    public class InstituicaoModel
    {
        protected InstituicaoModel() { }

        public InstituicaoModel(string nomeFantasia, string cnpj, bool ativa)
        {
            Id = Guid.NewGuid();
            NomeFantasia = nomeFantasia;
            Cnpj = cnpj;
            Ativa = ativa;
            CriadaEm = DateTime.UtcNow;
            AtualizadaEm = DateTime.UtcNow;
        }

        public Guid Id { get; set; }
        public string NomeFantasia { get; set; } = null;
        public string Cnpj { get; set; } = null;
        public bool Ativa { get; set; }
        public DateTime CriadaEm { get; set; }
        public DateTime AtualizadaEm { get; set; }
    }
}
