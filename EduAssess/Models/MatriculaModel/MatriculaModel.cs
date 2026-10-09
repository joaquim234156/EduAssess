using System.Text.Json.Serialization;

namespace EduAssess.Models
{
    public class MatriculaModel
    {
        protected MatriculaModel()
        {
            Notas = new List<NotaModel>();
        }

        public MatriculaModel(Guid usuarioId, Guid turmaId)
        {
            Id = Guid.NewGuid();
            UsuarioId = usuarioId;
            TurmaId = turmaId;
            DataMatricula = DateTime.UtcNow;
            DataExpiracao = DataMatricula.AddYears(1);
            Ativa = true;

            Notas = new List<NotaModel>();
        }

        public Guid Id { get; init; }
        public Guid UsuarioId { get; private set; }
        public Guid TurmaId { get; private set; }
        public DateTime DataMatricula { get; set; }
        public DateTime DataExpiracao { get; set; }
        public bool Ativa { get; set; }

        public UserModel Usuario { get; private set; } = null!;
        public TurmaModel Turma { get; private set; } = null!;

        public ICollection<NotaModel> Notas { get; private set; }

        public void Update(Guid usuarioId, Guid turmaId)
        {
            UsuarioId = usuarioId;
            TurmaId = turmaId;
        }

        public void Disable()
        {
            Ativa = false;
        }
    }
}
