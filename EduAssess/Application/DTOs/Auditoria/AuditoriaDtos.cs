using EduAssess.Models.Global;
using EduAssess.Models.Local;

namespace EduAssess.Application.DTOs.Auditoria
{
    /// <summary>Quem está consultando o histórico. InstitutionIds nulo = todas (só vale para o Admin).</summary>
    public sealed record AuditViewer(Guid UserId, UserRole Role, IReadOnlyCollection<Guid>? InstitutionIds);

    public sealed class AuditFilter
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public AcaoAuditoria? Acao { get; set; }
        public CategoriaAuditoria? Categoria { get; set; }
        public string? Entidade { get; set; }
        public string? Search { get; set; }
        public Guid? InstituicaoId { get; set; }
        /// <summary>true = só o que os alunos fizeram (testes, questões respondidas).</summary>
        public bool OnlyStudentActivity { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public sealed record AuditRowDto(
        Guid Id, DateTime OcorridoEm, string Acao, string Categoria, string Entidade, string? EntidadeId,
        string? EntidadeRotulo, string? Detalhes, string AtorNome, string AtorPerfil, string? AlunoNome, string? Ip);

    public sealed record AuditPageDto(List<AuditRowDto> Itens, int Total, int Pagina, int TamanhoPagina);

    public sealed record StudentActivitySummary(int Alunos, int Respostas, int Acertos, int Testes, List<QuestionActivityRow> Questoes);

    public sealed record QuestionActivityRow(string QuestaoId, string Rotulo, int Respostas, int Alunos, int Acertos, DateTime UltimoAcesso);
}
