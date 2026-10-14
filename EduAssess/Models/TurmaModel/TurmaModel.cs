namespace EduAssess.Models.TurmaModel
{
    public class TurmaModel
    {
        public TurmaModel(string nome, Guid cursoId, Guid professorId, int periodoEmSemestre)
        {
            Id = Guid.NewGuid();
            Nome = nome;
            CursoId = cursoId;
            ProfessorId = professorId;
            PeriodoEmSemestre = periodoEmSemestre;
            Ativo = true;
            
        }
        public Guid Id { get; init; }
        public string Nome { get; private set; }
        public Guid CursoId { get; private set; }
        public Guid ProfessorId { get; private set; }
        public int  PeriodoEmSemestre { get; private set; }
        public bool Ativo { get; private set; }
        public void Update(string nome, Guid cursoId, Guid professorId, int periodoEmSemestre)
        {
            Nome = nome;
            CursoId = cursoId;
            ProfessorId = professorId;
            PeriodoEmSemestre = periodoEmSemestre;
        }
        public void Disable()
        {
            Ativo = false;
        }
    }
}
