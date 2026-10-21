namespace EduAssess.Models
{
    public class DisciplinaModel
    {
        protected DisciplinaModel() { }

        public DisciplinaModel(string nome, string descricao, Guid professorId)
        {
            Id = Guid.NewGuid();
            ProfessorId = professorId;
            Nome = nome;
            Descricao = descricao;
            Ativa = true;
        }

        public Guid Id { get; init; }
        public Guid ProfessorId{ get; private set;}
        public string Nome { get; private set; } = null!;
        public string Descricao { get; private set; } = null!;
        public bool Ativa {get; private set;}

        public void UpdateDisciplina(string nome, string descricao, Guid professorId)
        {
            Nome = nome;
            Descricao = descricao;
            ProfessorId = professorId;
        }
        public void Disable()
        {
            Ativa = false;
        }

        public void Enable()
        {
            Ativa = true;
        }
    }
}
