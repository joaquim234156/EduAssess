namespace EduAssess.Models
{
    public class NotaModel
    {
        protected NotaModel() { }

        public NotaModel(Guid matriculaId, Guid usuarioId, Guid disciplinaId, string descricao, decimal valor)
        {
            Id = Guid.NewGuid();
            MatriculaId = matriculaId;
            UsuarioId = usuarioId;
            DisciplinaId = disciplinaId;
            Valor = valor;
            Descricao = descricao;
            DataLancamento = DateTime.UtcNow;
        }

        public Guid Id { get; init; }
        public Guid UsuarioId { get; set; }
        public Guid DisciplinaId { get; set; }
        public Guid MatriculaId { get; private set; }
        public string Descricao { get; private set; } = null!;
        public decimal Valor { get; private set; }
        public DateTime DataLancamento { get; private set; }

        public MatriculaModel Matricula { get; private set; } = null!;
    }
}