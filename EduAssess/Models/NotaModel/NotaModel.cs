namespace EduAssess.Models
{
    public class NotaModel
    {
        protected NotaModel() { }

        public NotaModel(Guid matriculaId, string descricao, decimal valor)
        {
            Id = Guid.NewGuid();
            MatriculaId = matriculaId;
            Descricao = descricao;
            Valor = valor;
            DataLancamento = DateTime.UtcNow;
        }

        public Guid Id { get; init; }
        public Guid MatriculaId { get; private set; }
        public string Descricao { get; private set; }
        public decimal Valor { get; private set; }
        public DateTime DataLancamento { get; private set; }

        public MatriculaModel Matricula { get; private set; } = null!;
    }
}