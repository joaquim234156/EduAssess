namespace EduAssess.Models
{
    public class CursoModel
    {
        protected CursoModel() { }

        public CursoModel(string nome, string descricao, int duracaoEmAnos, Guid? instituicaoId = null)
        {
            Id = Guid.NewGuid();
            InstituicaoId = instituicaoId;
            Nome = nome;
            Descricao = descricao;
            DuracaoEmAnos = duracaoEmAnos;
            Ativo = true;
        }

        public Guid Id { get; init; }
        public Guid? InstituicaoId { get; private set; }
        public string Nome { get; private set; } = string.Empty;
        public string Descricao { get; private set; } = string.Empty;
        public int DuracaoEmAnos { get; private set; }
        public bool Ativo { get; private set; }

        public void Update(string nome, string descricao, int duracaoEmAnos)
        {
            Nome = nome;
            Descricao = descricao;
            DuracaoEmAnos = duracaoEmAnos;
        }
        public void Disable()
        {
            Ativo = false;
        }

        public void Enable()
        {
            Ativo = true;
        }
    }
}
