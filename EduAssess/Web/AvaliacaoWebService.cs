using EduAssess.Data;
using EduAssess.Models;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Web;

/// <summary>
/// Provas e simulados. Mesmo padrão dos outros serviços do painel: cada método revalida a sessão
/// no banco e abre um DbContext curto.
///  • Equipe (professor, gestor, administrador): criar, editar, montar, aplicar e apagar avaliações.
///    Professor altera só as dele; gestor e administrador alteram todas.
///  • Aluno: vê as avaliações "Aplicadas" e responde em /praticar (as respostas não são gravadas).
/// </summary>
public sealed class AvaliacaoWebService(IDbContextFactory<AppDbContext> dbFactory, UiSession session)
{
    private static readonly UserRole[] Teaching = [UserRole.Admin, UserRole.Gestor, UserRole.Professor];
    private static readonly UserRole[] Learners = [UserRole.Aluno, UserRole.Professor, UserRole.Gestor, UserRole.Admin];

    private static bool IsStaff(UserSummary me) => me.Role is UserRole.Admin or UserRole.Gestor;

    // ───────────────────────── Equipe ─────────────────────────

    /// <summary>Disciplinas disponíveis no formulário: as do professor, ou todas para a gestão.</summary>
    public async Task<List<DisciplineOption>> GetDisciplinesAsync()
    {
        var me = await session.RequireAsync(Teaching);
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = me.Id;
        var query = db.Disciplinas.AsNoTracking().Where(d => d.Ativa);
        if (!IsStaff(me))
            query = query.Where(d => d.ProfessorId == id);
        return await query.OrderBy(d => d.Nome).Select(d => new DisciplineOption(d.Id, d.Nome)).ToListAsync();
    }

    /// <summary>Lista os cartões. sort: "recent" (data de criação), "title" ou "questions".</summary>
    public async Task<List<ExamCard>> GetExamsAsync(bool onlyMine, string? search, StatusAvaliacao? status, string sort)
    {
        var me = await session.RequireAsync(Teaching);
        await using var db = await dbFactory.CreateDbContextAsync();
        var myId = me.Id;

        var query = db.Avaliacoes.AsNoTracking().Where(a => a.Ativa);
        if (onlyMine)
            query = query.Where(a => a.AutorId == myId);
        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var rows = await query.ToListAsync();
        return (await BuildCardsAsync(db, rows, me))
            .Where(c => string.IsNullOrWhiteSpace(search)
                || Contains(c.Title, search) || Contains(c.Code, search)
                || Contains(c.DisciplineName, search) || Contains(c.AuthorName, search))
            .OrderBy(c => 0)
            .ThenBy(c => sort == "title" ? c.Title : "")
            .ThenByDescending(c => sort == "questions" ? c.QuestionCount : 0)
            .ThenByDescending(c => sort is "title" or "questions" ? DateTime.MinValue : c.CreatedAt)
            .ToList();
    }

    public async Task<ExamDetail> GetExamAsync(Guid id)
    {
        var me = await session.RequireAsync(Teaching);
        await using var db = await dbFactory.CreateDbContextAsync();
        var exam = await db.Avaliacoes.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.Ativa)
            ?? throw new InvalidOperationException("A avaliação não foi encontrada.");
        var card = (await BuildCardsAsync(db, [exam], me)).First();
        var questions = await LoadQuestionsAsync(db, id, card.DisciplineName);
        return new ExamDetail(card, questions);
    }

    /// <summary>Cria (id nulo) ou edita uma avaliação e devolve o ID dela.</summary>
    public async Task<Guid> SaveExamAsync(Guid? id, ExamForm form)
    {
        var me = await session.RequireAsync(Teaching);
        var title = (form.Title ?? "").Trim();
        var details = (form.Details ?? "").Trim();
        if (title.Length is < 3 or > 80)
            throw new ArgumentException("O título deve ter entre 3 e 80 caracteres.");
        if (details.Length > 500)
            throw new ArgumentException("Os detalhes devem ter até 500 caracteres.");
        if (form.Periodicity != PeriodicidadeAvaliacao.Unica && form.Duration is < 1 or > 60)
            throw new ArgumentException("Informe a duração entre 1 e 60.");
        var duration = form.Periodicity == PeriodicidadeAvaliacao.Unica ? 0 : form.Duration;

        await using var db = await dbFactory.CreateDbContextAsync();
        var myId = me.Id;
        var staff = IsStaff(me);

        if (id is null)
        {
            var discipline = await db.Disciplinas.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == form.DisciplineId && d.Ativa && (staff || d.ProfessorId == myId))
                ?? throw new ArgumentException("Selecione uma disciplina.");

            var code = await NewCodeAsync(db, discipline.Nome);
            var exam = new AvaliacaoModel(code, title, form.Type, me.Id, discipline.Id, details, form.Periodicity, duration);
            db.Avaliacoes.Add(exam);
            await db.SaveChangesAsync();
            return exam.Id;
        }

        var existing = await db.Avaliacoes.FirstOrDefaultAsync(a => a.Id == id && a.Ativa && (staff || a.AutorId == myId))
            ?? throw new InvalidOperationException("A avaliação não foi encontrada ou você não pode alterá-la.");
        existing.Update(title, form.Type, details, form.Periodicity, duration);
        await db.SaveChangesAsync();
        return existing.Id;
    }

    /// <summary>Aplica (libera para os alunos) ou encerra a aplicação. Só aplica se tiver questões.</summary>
    public async Task SetAppliedAsync(Guid id, bool applied)
    {
        var me = await session.RequireAsync(Teaching);
        await using var db = await dbFactory.CreateDbContextAsync();
        var exam = await FindEditableAsync(db, id, me);

        if (applied)
        {
            var linked = await db.AvaliacoesQuestoes.Where(l => l.AvaliacaoId == id).Select(l => l.QuestaoId).ToListAsync();
            var activeCount = await db.Questoes.CountAsync(q => q.Ativa && linked.Contains(q.Id));
            if (activeCount == 0)
                throw new InvalidOperationException("Adicione ao menos uma questão antes de aplicar.");
            exam.Aplicar();
        }
        else
        {
            exam.Encerrar();
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteExamAsync(Guid id)
    {
        var me = await session.RequireAsync(Teaching);
        await using var db = await dbFactory.CreateDbContextAsync();
        var exam = await FindEditableAsync(db, id, me);
        exam.Disable();
        await db.SaveChangesAsync();
    }

    /// <summary>Questões do banco (da mesma disciplina) que ainda não estão na avaliação.</summary>
    public async Task<List<QuestionRow>> GetBankAsync(Guid examId, string? search)
    {
        var me = await session.RequireAsync(Teaching);
        await using var db = await dbFactory.CreateDbContextAsync();
        var exam = await FindEditableAsync(db, examId, me);
        var discipline = await db.Disciplinas.AsNoTracking().FirstAsync(d => d.Id == exam.DisciplinaId);

        var already = await db.AvaliacoesQuestoes.Where(l => l.AvaliacaoId == examId).Select(l => l.QuestaoId).ToListAsync();
        var rows = await db.Questoes.AsNoTracking()
            .Where(q => q.Ativa && q.DisciplinaId == exam.DisciplinaId && !already.Contains(q.Id))
            .OrderBy(q => q.Assunto).Take(200).ToListAsync();

        return rows.Select(q => QuestaoWebService.ToRow(q, discipline.Nome))
            .Where(r => string.IsNullOrWhiteSpace(search) || Contains(r.Statement, search) || Contains(r.Topic, search))
            .ToList();
    }

    public async Task AddQuestionsAsync(Guid examId, IEnumerable<Guid> questionIds)
    {
        var me = await session.RequireAsync(Teaching);
        await using var db = await dbFactory.CreateDbContextAsync();
        var exam = await FindEditableAsync(db, examId, me);

        var wanted = questionIds.Distinct().ToList();
        if (wanted.Count == 0)
            return;

        // Só entram questões ativas DA MESMA DISCIPLINA da avaliação.
        var valid = await db.Questoes.AsNoTracking()
            .Where(q => q.Ativa && q.DisciplinaId == exam.DisciplinaId && wanted.Contains(q.Id))
            .Select(q => q.Id).ToListAsync();
        var already = await db.AvaliacoesQuestoes.Where(l => l.AvaliacaoId == examId).ToListAsync();
        var next = already.Count == 0 ? 1 : already.Max(l => l.Ordem) + 1;

        foreach (var qid in valid.Where(v => already.All(l => l.QuestaoId != v)))
            db.AvaliacoesQuestoes.Add(new AvaliacaoQuestaoModel(examId, qid, next++));

        await db.SaveChangesAsync();
    }

    public async Task RemoveQuestionAsync(Guid examId, Guid questionId)
    {
        var me = await session.RequireAsync(Teaching);
        await using var db = await dbFactory.CreateDbContextAsync();
        await FindEditableAsync(db, examId, me);

        var link = await db.AvaliacoesQuestoes.FirstOrDefaultAsync(l => l.AvaliacaoId == examId && l.QuestaoId == questionId);
        if (link is not null)
        {
            db.AvaliacoesQuestoes.Remove(link);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Sobe (-1) ou desce (+1) uma questão na ordem da avaliação.</summary>
    public async Task MoveQuestionAsync(Guid examId, Guid questionId, int direction)
    {
        var me = await session.RequireAsync(Teaching);
        await using var db = await dbFactory.CreateDbContextAsync();
        await FindEditableAsync(db, examId, me);

        var links = await db.AvaliacoesQuestoes.Where(l => l.AvaliacaoId == examId).OrderBy(l => l.Ordem).ToListAsync();
        var index = links.FindIndex(l => l.QuestaoId == questionId);
        var target = index + Math.Sign(direction);
        if (index < 0 || target < 0 || target >= links.Count)
            return;

        // Renumera tudo em sequência para a ordem nunca ficar com buracos ou repetida.
        (links[index], links[target]) = (links[target], links[index]);
        for (var i = 0; i < links.Count; i++)
            links[i].SetOrdem(i + 1);

        await db.SaveChangesAsync();
    }

    // ───────────────────────── Aluno ─────────────────────────

    /// <summary>Avaliações aplicadas (e com ao menos uma questão ativa).</summary>
    public async Task<List<PracticeExam>> GetAppliedAsync()
    {
        await session.RequireAsync(Learners);
        await using var db = await dbFactory.CreateDbContextAsync();
        var rows = await db.Avaliacoes.AsNoTracking()
            .Where(a => a.Ativa && a.Status == StatusAvaliacao.Aplicada)
            .OrderByDescending(a => a.CriadaEm).ToListAsync();
        return await ToPracticeExamsAsync(db, rows);
    }

    /// <summary>Procura uma avaliação aplicada pelo código (ex.: PSI-26-10-9GN).</summary>
    public async Task<PracticeExam?> FindAppliedByCodeAsync(string? code)
    {
        await session.RequireAsync(Learners);
        var normalized = (code ?? "").Trim().ToUpperInvariant();
        if (normalized.Length is < 6 or > 20)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.Avaliacoes.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Ativa && a.Status == StatusAvaliacao.Aplicada && a.Codigo == normalized);
        return row is null ? null : (await ToPracticeExamsAsync(db, [row])).FirstOrDefault();
    }

    /// <summary>Monta as questões da avaliação na ordem definida; só as alternativas são embaralhadas.</summary>
    public async Task<List<PracticeQuestion>> BuildExamAsync(Guid examId)
    {
        await session.RequireAsync(Learners);
        await using var db = await dbFactory.CreateDbContextAsync();
        var exam = await db.Avaliacoes.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == examId && a.Ativa && a.Status == StatusAvaliacao.Aplicada)
            ?? throw new InvalidOperationException("Essa avaliação não está disponível.");

        var questions = await LoadQuestionsAsync(db, exam.Id, "");
        var result = new List<PracticeQuestion>();
        foreach (var item in questions)
        {
            var q = item.Question;
            if (q.CorrectIndex < 0)
                continue;
            var correctText = q.Alternatives[q.CorrectIndex];
            var shuffled = q.Alternatives.OrderBy(_ => Random.Shared.Next()).ToList();
            result.Add(new PracticeQuestion(q.Id, q.Topic, q.Statement, shuffled, shuffled.IndexOf(correctText), q.Explanation, q.Difficulty));
        }

        return result;
    }

    // ───────────────────────── Auxiliares ─────────────────────────

    private static bool Contains(string text, string term) =>
        text.Contains(term.Trim(), StringComparison.OrdinalIgnoreCase);

    private static async Task<AvaliacaoModel> FindEditableAsync(AppDbContext db, Guid id, UserSummary me)
    {
        var staff = IsStaff(me);
        var myId = me.Id;
        return await db.Avaliacoes.FirstOrDefaultAsync(a => a.Id == id && a.Ativa && (staff || a.AutorId == myId))
            ?? throw new InvalidOperationException("A avaliação não foi encontrada ou você não pode alterá-la.");
    }

    /// <summary>Gera um código que ainda não existe (tenta de novo no raro caso de repetir).</summary>
    private static async Task<string> NewCodeAsync(AppDbContext db, string disciplineName)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = AvaliacaoModel.GerarCodigo(disciplineName, DateTime.Now);
            if (!await db.Avaliacoes.AnyAsync(a => a.Codigo == code))
                return code;
        }
        throw new InvalidOperationException("Não foi possível gerar um código único. Tente novamente.");
    }

    private static async Task<List<ExamCard>> BuildCardsAsync(AppDbContext db, List<AvaliacaoModel> rows, UserSummary me)
    {
        if (rows.Count == 0)
            return new();

        var ids = rows.Select(r => r.Id).ToList();
        var links = await db.AvaliacoesQuestoes.AsNoTracking().Where(l => ids.Contains(l.AvaliacaoId))
            .Select(l => new { l.AvaliacaoId, l.QuestaoId }).ToListAsync();
        var questionIds = links.Select(l => l.QuestaoId).Distinct().ToList();
        var activeQuestions = (await db.Questoes.AsNoTracking().Where(q => q.Ativa && questionIds.Contains(q.Id))
            .Select(q => q.Id).ToListAsync()).ToHashSet();

        var disciplines = (await db.Disciplinas.AsNoTracking().Select(d => new { d.Id, d.Nome }).ToListAsync())
            .ToDictionary(d => d.Id, d => d.Nome);
        var users = (await db.Users.AsNoTracking().Select(u => new { u.Id, u.Name }).ToListAsync())
            .ToDictionary(u => u.Id, u => u.Name);
        var staff = IsStaff(me);

        return rows.Select(a => new ExamCard(
            a.Id, a.Codigo, a.Titulo, a.Tipo, a.DisciplinaId,
            disciplines.TryGetValue(a.DisciplinaId, out var d) ? d : "—",
            a.AutorId, users.TryGetValue(a.AutorId, out var u) ? u : "—",
            a.CriadaEm, links.Count(l => l.AvaliacaoId == a.Id && activeQuestions.Contains(l.QuestaoId)),
            a.Status, a.Detalhes, a.Periodicidade, a.Duracao,
            staff || a.AutorId == me.Id)).ToList();
    }

    private static async Task<List<ExamQuestion>> LoadQuestionsAsync(AppDbContext db, Guid examId, string disciplineName)
    {
        var links = await db.AvaliacoesQuestoes.AsNoTracking().Where(l => l.AvaliacaoId == examId)
            .OrderBy(l => l.Ordem).ToListAsync();
        var ids = links.Select(l => l.QuestaoId).ToList();
        var questions = (await db.Questoes.AsNoTracking().Where(q => q.Ativa && ids.Contains(q.Id)).ToListAsync())
            .ToDictionary(q => q.Id);

        return links.Where(l => questions.ContainsKey(l.QuestaoId))
            .Select(l => new ExamQuestion(l.Ordem, QuestaoWebService.ToRow(questions[l.QuestaoId], disciplineName)))
            .ToList();
    }

    private static async Task<List<PracticeExam>> ToPracticeExamsAsync(AppDbContext db, List<AvaliacaoModel> rows)
    {
        if (rows.Count == 0)
            return new();

        var ids = rows.Select(r => r.Id).ToList();
        var links = await db.AvaliacoesQuestoes.AsNoTracking().Where(l => ids.Contains(l.AvaliacaoId))
            .Select(l => new { l.AvaliacaoId, l.QuestaoId }).ToListAsync();
        var questionIds = links.Select(l => l.QuestaoId).Distinct().ToList();
        var active = (await db.Questoes.AsNoTracking().Where(q => q.Ativa && questionIds.Contains(q.Id))
            .Select(q => q.Id).ToListAsync()).ToHashSet();
        var disciplines = (await db.Disciplinas.AsNoTracking().Select(d => new { d.Id, d.Nome }).ToListAsync())
            .ToDictionary(d => d.Id, d => d.Nome);
        var users = (await db.Users.AsNoTracking().Select(u => new { u.Id, u.Name }).ToListAsync())
            .ToDictionary(u => u.Id, u => u.Name);

        return rows
            .Select(a => new PracticeExam(
                a.Id, a.Codigo, a.Titulo, disciplines.TryGetValue(a.DisciplinaId, out var d) ? d : "—",
                users.TryGetValue(a.AutorId, out var u) ? u : "—",
                links.Count(l => l.AvaliacaoId == a.Id && active.Contains(l.QuestaoId)), a.Tipo))
            .Where(p => p.QuestionCount > 0)
            .ToList();
    }
}
