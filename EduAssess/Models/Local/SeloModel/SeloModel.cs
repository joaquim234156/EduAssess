namespace EduAssess.Models.Local
{
    /// <summary>Códigos dos tipos de selo que o sistema conhece. Novos tipos entram só como linha no catálogo.</summary>
    public static class SeloCodigos
    {
        public const string IA = "IA";
        public const string Enade = "ENADE";
        public const string Enamed = "ENAMED";
        public const string Enem = "ENEM";
        public const string Vestibular = "VESTIBULAR";
    }

    /// <summary>
    /// NOVO: catálogo de tipos de selo (uma linha por tipo). Os flags dizem ao formulário quais campos
    /// pedir (ano, instituição/detalhe). Adicionar um selo futuro = inserir uma linha, sem mudar o esquema.
    /// </summary>
    public class SeloTipoModel
    {
        protected SeloTipoModel() { }

        public SeloTipoModel(string codigo, string nome, string cor, int ordem, string? descricao = null,
            bool permiteAno = false, bool exigeAno = false, bool permiteDetalhe = false, bool exigeDetalhe = false, string? rotuloDetalhe = null)
        {
            Id = Guid.NewGuid();
            Codigo = codigo.Trim().ToUpperInvariant();
            Nome = nome.Trim();
            Cor = cor;
            Ordem = ordem;
            Descricao = descricao;
            PermiteAno = permiteAno || exigeAno;
            ExigeAno = exigeAno;
            PermiteDetalhe = permiteDetalhe || exigeDetalhe;
            ExigeDetalhe = exigeDetalhe;
            RotuloDetalhe = rotuloDetalhe;
            Ativo = true;
        }

        public Guid Id { get; init; }
        public string Codigo { get; private set; } = string.Empty;
        public string Nome { get; private set; } = string.Empty;
        public string? Descricao { get; private set; }
        /// <summary>Chave de cor do selo (violet, blue, green, amber, red, gray): vira a classe CSS badge-*.</summary>
        public string Cor { get; private set; } = "violet";
        public int Ordem { get; private set; }
        public bool PermiteAno { get; private set; }
        public bool ExigeAno { get; private set; }
        public bool PermiteDetalhe { get; private set; }
        public bool ExigeDetalhe { get; private set; }
        public string? RotuloDetalhe { get; private set; }
        public bool Ativo { get; private set; }
    }

    /// <summary>
    /// NOVO: selo aplicado a uma questão. A chave (questão, tipo) impede duplicidade: um tipo por questão.
    /// Exibir = o professor decide se o selo aparece no banco, nas provas e para o aluno.
    /// </summary>
    public class QuestaoSeloModel
    {
        protected QuestaoSeloModel() { }

        public QuestaoSeloModel(Guid questaoId, Guid seloTipoId, int? ano, string? detalhe, bool exibir)
        {
            QuestaoId = questaoId;
            SeloTipoId = seloTipoId;
            Ano = ano;
            Detalhe = detalhe;
            Exibir = exibir;
            CriadoEm = DateTime.UtcNow;
        }

        public Guid QuestaoId { get; private set; }
        public Guid SeloTipoId { get; private set; }
        public int? Ano { get; private set; }
        public string? Detalhe { get; private set; }
        public bool Exibir { get; private set; }
        public DateTime CriadoEm { get; private set; }
        public QuestaoModel? Questao { get; private set; }
        public SeloTipoModel SeloTipo { get; private set; } = null!;

        public void Update(int? ano, string? detalhe, bool exibir)
        {
            Ano = ano;
            Detalhe = detalhe;
            Exibir = exibir;
        }
    }
}
