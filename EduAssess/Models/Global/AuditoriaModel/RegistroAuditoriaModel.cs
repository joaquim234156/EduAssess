namespace EduAssess.Models.Global
{
    public enum AcaoAuditoria
    {
        Criacao = 1,
        Edicao = 2,
        Exclusao = 3,
        StatusAlterado = 4,
        Acesso = 5,             // login com sucesso
        FalhaAcesso = 6,        // tentativa de login recusada
        Saida = 7,              // logout
        TesteIniciado = 8,
        QuestaoRespondida = 9,
        TesteConcluido = 10,
        MaterialVisualizado = 11, // reservado: o sistema ainda não tem módulo de materiais
    }

    public enum CategoriaAuditoria
    {
        Gestao = 1,       // cadastros, turmas, matrículas, notas, instituição
        Conteudo = 2,     // questões, selos, avaliações
        Acesso = 3,       // login/logout
        Atividade = 4,    // o que o aluno fez em questões e testes
        Desempenho = 5,   // escolhas do aluno sobre os próprios simulados
        Sistema = 6,
    }

    /// <summary>
    /// NOVO: registro de auditoria. SOMENTE INSERÇÃO: todas as propriedades têm setter privado, o
    /// interceptor do EF recusa alterar/excluir e um gatilho do PostgreSQL bloqueia UPDATE, DELETE e TRUNCATE.
    /// Não há chave estrangeira de propósito: o histórico sobrevive mesmo se a entidade citada sumir.
    /// </summary>
    public class RegistroAuditoriaModel
    {
        protected RegistroAuditoriaModel() { }

        public RegistroAuditoriaModel(AcaoAuditoria acao, CategoriaAuditoria categoria, string entidade,
            Guid? atorId, string atorNome, string atorPerfil, Guid? instituicaoId)
        {
            Id = Guid.NewGuid();
            OcorridoEm = DateTime.UtcNow;
            Acao = acao;
            Categoria = categoria;
            Entidade = Cut(entidade, 60)!;
            AtorId = atorId;
            AtorNome = Cut(atorNome, 150)!;
            AtorPerfil = Cut(atorPerfil, 40)!;
            InstituicaoId = instituicaoId;
        }

        public Guid Id { get; private set; }
        public DateTime OcorridoEm { get; private set; }
        public AcaoAuditoria Acao { get; private set; }
        public CategoriaAuditoria Categoria { get; private set; }
        public string Entidade { get; private set; } = string.Empty;
        public string? EntidadeId { get; private set; }
        public string? EntidadeRotulo { get; private set; }
        public string? Detalhes { get; private set; }
        public Guid? AtorId { get; private set; }
        public string AtorNome { get; private set; } = string.Empty;
        public string AtorPerfil { get; private set; } = string.Empty;
        public Guid? InstituicaoId { get; private set; }
        /// <summary>Professor dono da turma/conteúdo envolvido: é por ele que o professor enxerga o próprio histórico.</summary>
        public Guid? ProfessorResponsavelId { get; private set; }
        /// <summary>Aluno de quem é a atividade (testes, questões respondidas, notas).</summary>
        public Guid? AlunoId { get; private set; }
        public string? Ip { get; private set; }

        public RegistroAuditoriaModel Com(string? entidadeId = null, string? rotulo = null, string? detalhes = null,
            Guid? professorId = null, Guid? alunoId = null, string? ip = null)
        {
            EntidadeId = Cut(entidadeId, 100);
            EntidadeRotulo = Cut(rotulo, 200);
            Detalhes = Cut(detalhes, 800);
            ProfessorResponsavelId = professorId;
            AlunoId = alunoId;
            Ip = Cut(ip, 64);
            return this;
        }

        private static string? Cut(string? v, int max) => v is null ? null : v.Length <= max ? v : v[..max];
    }
}
