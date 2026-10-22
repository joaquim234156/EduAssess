
namespace EduAssess.Models
{
    public class TurmaModel
    {
        protected TurmaModel() { }
        public TurmaModel(string nome, Guid professorId, Guid cursoId)
        {
            Id = Guid.NewGuid();
            ProfessorId = professorId;
            CursoId = cursoId;
            Periodo = 1;
            Nome = nome;
            Ativo = true;
        }

        public Guid Id { get; init; }
        public Guid ProfessorId { get; private set; }
        public Guid CursoId { get; private set; }
        public int Periodo { get; private set; } = 0;
        public string Nome { get; private set; }
        public bool Ativo { get; private set; }
        public CursoModel Curso { get; private set; } = null!;
        public UserModel Professor { get; private set; } = null!;
    }
}
