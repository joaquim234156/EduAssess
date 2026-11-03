using EduAssess.Extensions;
using Microsoft.EntityFrameworkCore.Sqlite.Update.Internal;

namespace EduAssess.Models.Global
{
    public class InstituicaoModel
    {
        protected InstituicaoModel() { }

        public InstituicaoModel(string nome, string cnpj, string email, string telefone, string endereco)
        {
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
        public string Nome { get; set; } = null;
        public string Cnpj { get; set; } = null;
        public string Email { get; set; } = null;
        public string Telefone { get; set; } = null;
        public string Endereco { get; set; } = null;
        public bool Ativa { get; set; }
        public string Slug { get; set; } = null;
        public DateTime CriadaEm { get; set; }
        public DateTime AtualizadaEm { get; set; }
        public List<UnidadeModel> Unidades { get; set; } = new();

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
    }
}
