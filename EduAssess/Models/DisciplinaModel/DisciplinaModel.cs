namespace EduAssess.Models
{
    public class DisciplinaModel
    {
        protected DisciplinaModel() { }

        public DisciplinaModel(string nome, string descricao)
        {
            Id = Guid.NewGuid();
            Nome = nome;
            Descricao = descricao;
        }

        public Guid Id { get; init; }
        public string Nome { get; private set; } = null!;
        public string Descricao { get; private set; } = null!;
    }
}
