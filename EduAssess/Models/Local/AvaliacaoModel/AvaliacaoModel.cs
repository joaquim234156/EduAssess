using System.Globalization;
using System.Text;

namespace EduAssess.Models.Local
{
    public enum TipoAvaliacao
    {
        Prova = 0,
        Simulado = 1,
    }

    public enum PeriodicidadeAvaliacao
    {
        Unica = 0,
        Mensal = 1,
        Bimestral = 2,
        Trimestral = 3,
        Semestral = 4,
        Anual = 5,
    }

    public enum StatusAvaliacao
    {
        Rascunho = 0,   // ainda sendo montada: só a equipe vê
        Aplicada = 1,   // liberada: os alunos podem responder em "Praticar"
    }

    /// <summary>
    /// Avaliação (prova ou simulado): um conjunto de questões do banco de uma disciplina.
    /// O Codigo (ex.: PSI-26-10-9GN) é o "ID" mostrado no cartão e usado para compartilhar.
    /// </summary>
    public class AvaliacaoModel
    {
        protected AvaliacaoModel()
        {
            Questoes = new List<AvaliacaoQuestaoModel>();
        }

        public AvaliacaoModel(
            string codigo, string titulo, TipoAvaliacao tipo, Guid autorId, Guid disciplinaId,
            string detalhes, PeriodicidadeAvaliacao periodicidade, int duracao)
        {
            Id = Guid.NewGuid();
            Codigo = codigo;
            Titulo = titulo;
            Tipo = tipo;
            AutorId = autorId;
            DisciplinaId = disciplinaId;
            Detalhes = detalhes;
            Periodicidade = periodicidade;
            Duracao = duracao;
            Status = StatusAvaliacao.Rascunho;
            CriadaEm = DateTime.UtcNow;
            Ativa = true;
            Questoes = new List<AvaliacaoQuestaoModel>();
        }

        public Guid Id { get; init; }
        public string Codigo { get; private set; } = null!;
        public string Titulo { get; private set; } = null!;
        public TipoAvaliacao Tipo { get; private set; }
        public Guid AutorId { get; private set; }
        public Guid DisciplinaId { get; private set; }
        public string Detalhes { get; private set; } = string.Empty;
        public PeriodicidadeAvaliacao Periodicidade { get; private set; }
        public int Duracao { get; private set; }
        public StatusAvaliacao Status { get; private set; }
        public DateTime CriadaEm { get; private set; }
        public bool Ativa { get; private set; }
        public ICollection<AvaliacaoQuestaoModel> Questoes { get; private set; }

        public void Update(string titulo, TipoAvaliacao tipo, string detalhes, PeriodicidadeAvaliacao periodicidade, int duracao)
        {
            Titulo = titulo;
            Tipo = tipo;
            Detalhes = detalhes;
            Periodicidade = periodicidade;
            Duracao = duracao;
        }

        public void Aplicar() => Status = StatusAvaliacao.Aplicada;
        public void Encerrar() => Status = StatusAvaliacao.Rascunho;
        public void Disable() => Ativa = false;

        /// <summary>
        /// Monta o código: 3 letras da disciplina + ano + mês + 3 caracteres aleatórios
        /// (ex.: "Psicologia" em outubro de 2026 vira PSI-26-10-9GN). Não usa 0/O/1/I para não confundir.
        /// </summary>
        public static string GerarCodigo(string nomeDisciplina, DateTime agora)
        {
            var letras = new string((nomeDisciplina ?? "")
                .Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetter(c))
                .Select(char.ToUpperInvariant)
                .Take(3)
                .ToArray())
                .PadRight(3, 'X');

            const string alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var sufixo = new string(Enumerable.Range(0, 3)
                .Select(_ => alfabeto[Random.Shared.Next(alfabeto.Length)]).ToArray());

            return $"{letras}-{agora:yy}-{agora:MM}-{sufixo}";
        }
    }

    /// <summary>Ligação entre uma avaliação e uma questão do banco, com a ordem em que aparece.</summary>
    public class AvaliacaoQuestaoModel
    {
        protected AvaliacaoQuestaoModel() { }

        public AvaliacaoQuestaoModel(Guid avaliacaoId, Guid questaoId, int ordem)
        {
            AvaliacaoId = avaliacaoId;
            QuestaoId = questaoId;
            Ordem = ordem;
        }

        public Guid AvaliacaoId { get; private set; }
        public Guid QuestaoId { get; private set; }
        public int Ordem { get; private set; }
        public AvaliacaoModel Avaliacao { get; private set; } = null!;

        public void SetOrdem(int ordem) => Ordem = ordem;
    }
}
