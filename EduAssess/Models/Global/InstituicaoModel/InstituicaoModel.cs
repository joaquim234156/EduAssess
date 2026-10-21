using Microsoft.EntityFrameworkCore.Sqlite.Update.Internal;

namespace EduAssess.Models.Global
{
    public class InstituicaoModel
    {
        protected InstituicaoModel() { }

        public InstituicaoModel(string nomeFantasia, string slug, string cnpj, bool ativa)
        {
            Id = Guid.NewGuid();
            NomeFantasia = nomeFantasia;
            Slug = slug;
            Cnpj = cnpj;
            Ativa = ativa;
            CriadaEm = DateTime.UtcNow;
            AtualizadaEm = DateTime.UtcNow;
        }

        public Guid Id { get; set; }
        public string NomeFantasia { get; set; } = null;
        public string Slug { get; set; } = null;
        public string Cnpj { get; set; } = null;
        public bool Ativa { get; set; }
        public DateTime CriadaEm { get; set; }
        public DateTime AtualizadaEm { get; set; }
        public List<UnidadeModel> Unidades { get; set; } = new();
    }
}
