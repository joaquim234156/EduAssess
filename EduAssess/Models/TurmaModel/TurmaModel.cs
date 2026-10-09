namespace EduAssess.Models
{
    public class TurmaModel
    {
        protected TurmaModel() { }

        public TurmaModel(int cursoId, string nome, string descricao)
        {
            Id = Guid.NewGuid();
            CursoId = cursoId;
            Nome = nome;
            Descricao = descricao;
            Ativa = true;
        }

        public Guid Id { get; init; }
        public int CursoId { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public bool Ativa { get; set; }

        public void Update(int cursoId, string nome, string descricao)
        {
            CursoId = cursoId;
            Nome = nome;
            Descricao = descricao;
        }

        public void Disable()
        {
            Ativa = false;
        }
    }
}
