namespace EduAssess.Models
{
    /// <summary>
    /// Solicitação de contato enviada pelo formulário público "Vamos conversar".
    /// </summary>
    public class ContatoModel
    {
        protected ContatoModel() { }

        public ContatoModel(string nome, string email, string telefone, string instituicao, string papel, string mensagem)
        {
            Id = Guid.NewGuid();
            Nome = nome.Trim();
            Email = email.Trim().ToLowerInvariant();
            Telefone = telefone.Trim();
            Instituicao = instituicao.Trim();
            Papel = papel.Trim();
            Mensagem = mensagem.Trim();
            CriadoEm = DateTime.UtcNow;
            Lido = false;
        }

        public Guid Id { get; init; }
        public string Nome { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public string Telefone { get; private set; } = null!;
        public string Instituicao { get; private set; } = null!;
        public string Papel { get; private set; } = null!;
        public string Mensagem { get; private set; } = null!;
        public DateTime CriadoEm { get; private set; }
        public bool Lido { get; private set; }

        public void MarcarComoLido() => Lido = true;
    }
}
