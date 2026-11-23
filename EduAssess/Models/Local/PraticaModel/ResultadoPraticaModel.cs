namespace EduAssess.Models.Local
{
    public enum OrigemPratica
    {
        Treino = 0,     // treino livre por disciplina
        Simulado = 1,   // avaliação do tipo simulado aplicada por um professor
        Prova = 2,      // avaliação do tipo prova aplicada por um professor (continua sendo prática, NÃO é nota oficial)
    }

    /// <summary>
    /// NOVO: resultado de um treino/simulado feito pelo próprio aluno em "Praticar".
    /// Fica numa tabela À PARTE das notas oficiais (<see cref="EduAssess.Models.NotaModel"/>): não há
    /// caminho de código que copie este valor para Notas. A única coisa que o aluno muda é
    /// <see cref="Contabilizar"/> (setters privados garantem isso).
    /// </summary>
    public class ResultadoPraticaModel
    {
        protected ResultadoPraticaModel() { }

        public ResultadoPraticaModel(Guid instituicaoId, Guid usuarioId, OrigemPratica origem, Guid? avaliacaoId,
            Guid? disciplinaId, string titulo, string disciplinaNome, int totalQuestoes, int acertos)
        {
            if (totalQuestoes < 1) throw new ArgumentException("Total de questões inválido.");
            if (acertos < 0 || acertos > totalQuestoes) throw new ArgumentException("Acertos inválidos.");
            Id = Guid.NewGuid();
            InstituicaoId = instituicaoId;
            UsuarioId = usuarioId;
            Origem = origem;
            AvaliacaoId = avaliacaoId;
            DisciplinaId = disciplinaId;
            Titulo = titulo.Length > 120 ? titulo[..120] : titulo;
            DisciplinaNome = disciplinaNome.Length > 100 ? disciplinaNome[..100] : disciplinaNome;
            TotalQuestoes = totalQuestoes;
            Acertos = acertos;
            Nota = decimal.Round(acertos * 10m / totalQuestoes, 2);
            RealizadoEm = DateTime.UtcNow;
            Contabilizar = false;   // opt-in: só entra no desempenho se o aluno escolher
        }

        public Guid Id { get; private set; }
        public Guid InstituicaoId { get; private set; }
        public Guid UsuarioId { get; private set; }
        public OrigemPratica Origem { get; private set; }
        public Guid? AvaliacaoId { get; private set; }
        public Guid? DisciplinaId { get; private set; }
        public string Titulo { get; private set; } = string.Empty;
        public string DisciplinaNome { get; private set; } = string.Empty;
        public int TotalQuestoes { get; private set; }
        public int Acertos { get; private set; }
        public decimal Nota { get; private set; }
        public DateTime RealizadoEm { get; private set; }
        public bool Contabilizar { get; private set; }

        public void DefinirContabilizacao(bool contabilizar) => Contabilizar = contabilizar;
    }
}
