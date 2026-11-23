using EduAssess.Application.Services;
using EduAssess.Data;
using EduAssess.Models.Global;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Web;

/// <summary>
/// NOVO: grava e lê os resultados de treinos/simulados do aluno e registra a atividade dele para o histórico do professor.
/// Os resultados vivem na tabela ResultadosPratica, SEPARADA das notas oficiais: nada aqui lê ou escreve em Notas.
/// O aluno só consegue mudar um campo: "contabilizar no meu desempenho".
/// </summary>
public sealed class PracticeResultService(IDbContextFactory<AppDbContext> dbFactory, UiSession session, AuditoriaService audit)
{
    private static readonly UserRole[] OnlyStudent = [UserRole.Aluno];

    // ───────────────────────── Atividade do aluno (histórico do professor) ─────────────────────────

    /// <summary>Teste iniciado. Funcionários usando "Ver como aluno" não geram atividade de aluno.</summary>
    public async Task LogStartAsync(OrigemPratica origin, Guid? examId, Guid? disciplineId, string title)
    {
        var (me, instId) = await Who();
        if (me is null) return;
        var professorId = await OwnerOfTestAsync(examId, disciplineId);
        await audit.RegistrarAsync(AcaoAuditoria.TesteIniciado, CategoriaAuditoria.Atividade, examId is null ? "Disciplina" : "Avaliacao",
            Actor(me, instId), (examId ?? disciplineId)?.ToString(), title, $"{OriginLabel(origin)} iniciado", professorId, me.Id, instId);
    }

    public async Task LogAnswerAsync(Guid questionId, bool correct, string testTitle)
    {
        var (me, instId) = await Who();
        if (me is null) return;
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = await db.Questoes.AsNoTracking().Where(x => x.Id == questionId && x.InstituicaoId == instId)
            .Select(x => new { x.ProfessorId, x.Assunto, x.Enunciado }).FirstOrDefaultAsync();
        if (q is null) return;
        await audit.RegistrarAsync(AcaoAuditoria.QuestaoRespondida, CategoriaAuditoria.Atividade, "Questao", Actor(me, instId),
            questionId.ToString(), $"{q.Assunto}: {Cut(q.Enunciado, 60)}", $"{(correct ? "Acertou" : "Errou")} · {testTitle}",
            q.ProfessorId, me.Id, instId);
    }

    /// <summary>
    /// Conclui o teste: grava o resultado (não oficial, não contabilizado até o aluno escolher) e registra no histórico.
    /// Devolve null para quem não é aluno. A nota é calculada aqui a partir de acertos/total; nunca vem pronta do navegador.
    /// </summary>
    public async Task<Guid?> FinishAsync(OrigemPratica origin, Guid? examId, Guid? disciplineId, string title, string disciplineName, int total, int hits)
    {
        var (me, instId) = await Who();
        if (me is null || instId is null) return null;
        if (total is < 1 or > 100 || hits < 0 || hits > total)
            throw new ArgumentException("Resultado inválido.");

        await using var db = await dbFactory.CreateDbContextAsync();
        // Garante que a avaliação/disciplina citada é desta instituição (nada de ID arbitrário).
        if (examId is Guid e && !await db.Avaliacoes.AnyAsync(a => a.Id == e && a.InstituicaoId == instId && a.Ativa))
            throw new InvalidOperationException("Avaliação não encontrada.");
        if (disciplineId is Guid d && !await db.Disciplinas.AnyAsync(x => x.Id == d && x.InstituicaoId == instId && x.Ativa))
            throw new InvalidOperationException("Disciplina não encontrada.");

        var result = new ResultadoPraticaModel(instId.Value, me.Id, origin, examId, disciplineId, title, disciplineName, total, hits);
        db.ResultadosPratica.Add(result);
        await db.SaveChangesAsync();

        var professorId = await OwnerOfTestAsync(examId, disciplineId);
        await audit.RegistrarAsync(AcaoAuditoria.TesteConcluido, CategoriaAuditoria.Atividade, examId is null ? "Disciplina" : "Avaliacao",
            Actor(me, instId), (examId ?? disciplineId)?.ToString(), title, $"{hits}/{total} ({result.Nota:0.0}) · {OriginLabel(origin)}",
            professorId, me.Id, instId);
        return result.Id;
    }

    // ───────────────────────── Escolha do aluno: contabilizar ou não ─────────────────────────

    /// <summary>Muda SOMENTE o campo Contabilizar, e só de um resultado do próprio aluno (checa dono e instituição).</summary>
    public async Task SetCountedAsync(Guid resultId, bool counted)
    {
        var me = await session.RequireAsync(OnlyStudent);
        var instId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.ResultadosPratica.FirstOrDefaultAsync(r => r.Id == resultId && r.UsuarioId == me.Id && r.InstituicaoId == instId)
            ?? throw new InvalidOperationException("Resultado não encontrado.");
        if (row.Contabilizar == counted) return;
        row.DefinirContabilizacao(counted);
        await db.SaveChangesAsync();
    }

    public async Task<PracticeSummary> GetMineAsync(int? take = null)
    {
        var me = await session.RequireAsync(OnlyStudent);
        var instId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var all = db.ResultadosPratica.AsNoTracking().Where(r => r.UsuarioId == me.Id && r.InstituicaoId == instId);

        var counted = await all.Where(r => r.Contabilizar).Select(r => r.Nota).ToListAsync();
        var query = all.OrderByDescending(r => r.RealizadoEm);
        var rows = await (take is int t ? query.Take(t) : query).ToListAsync();
        return new PracticeSummary(
            rows.Select(r => new PracticeResultRow(r.Id, r.Titulo, r.DisciplinaNome, r.Origem, r.TotalQuestoes, r.Acertos, r.Nota, r.RealizadoEm, r.Contabilizar)).ToList(),
            counted.Count, counted.Count == 0 ? null : decimal.Round(counted.Average(), 2), counted.Sum(), await all.CountAsync());
    }

    // ───────────────────────── auxiliares ─────────────────────────

    /// <summary>Só alunos geram atividade/resultado; o resto (staff vendo como aluno) devolve null.</summary>
    private async Task<(UserSummary? Me, Guid? InstitutionId)> Who()
    {
        var me = await session.RequireAsync();
        return me.Role == UserRole.Aluno ? (me, session.InstitutionId) : (null, null);
    }

    private static Data.Audit.AuditActor Actor(UserSummary me, Guid? instId) =>
        new(me.Id, me.Name, UiFormat.RoleLabel(me.Role), instId, null);

    private async Task<Guid?> OwnerOfTestAsync(Guid? examId, Guid? disciplineId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (examId is Guid e)
            return await db.Avaliacoes.AsNoTracking().Where(a => a.Id == e).Select(a => (Guid?)a.AutorId).FirstOrDefaultAsync();
        if (disciplineId is Guid d)
            return await db.Disciplinas.AsNoTracking().Where(x => x.Id == d).Select(x => (Guid?)x.ProfessorId).FirstOrDefaultAsync();
        return null;
    }

    public static string OriginLabel(OrigemPratica o) => o switch { OrigemPratica.Simulado => "Simulado", OrigemPratica.Prova => "Prova", _ => "Treino" };
    private static string Cut(string v, int n) => v.Length <= n ? v : v[..n] + "…";
}
