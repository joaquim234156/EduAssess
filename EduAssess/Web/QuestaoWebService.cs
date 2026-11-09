using EduAssess.Application.Interfaces;
using EduAssess.Data;
using EduAssess.Models;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Web;

/// <summary>
/// Operações de questões usadas pelas telas Blazor:
///  • Professor: gerar com IA, revisar, salvar, listar e apagar as suas questões (/banco-questoes).
///  • Aluno: montar uma sessão de prática com questões embaralhadas (/praticar).
/// Segue o mesmo padrão do AcademicService: cada método revalida a sessão no banco e abre um
/// DbContext curto. As regras de posse são as mesmas das rotas da API (/questoes/*).
/// </summary>
public sealed class QuestaoWebService(
    IDbContextFactory<AppDbContext> dbFactory,
    UiSession session,
    IIaQuestaoService ia)
{
    private static readonly UserRole[] Learners = [UserRole.Aluno, UserRole.Professor, UserRole.Gestor, UserRole.Admin];

    // ───────────────────────── Equipe (professor, gestor e administrador) ─────────────────────────
    // Regra de posse (a mesma das rotas da API): o professor só mexe nas disciplinas e questões dele;
    // gestor e administrador trabalham em qualquer disciplina e em questões de qualquer autor.

    private static readonly UserRole[] Teaching = [UserRole.Admin, UserRole.Gestor, UserRole.Professor];

    private static bool IsStaff(UserSummary me) => me.Role is UserRole.Admin or UserRole.Gestor;

    /// <summary>Monta a linha de exibição de uma questão (alternativas em lista + índice da correta).</summary>
    public static QuestionRow ToRow(QuestaoModel q, string disciplineName)
    {
        var alternatives = new List<string> { q.Alternativa1, q.Alternativa2, q.Alternativa3, q.Alternativa4, q.Alternativa5 };
        return new QuestionRow(
            q.Id, q.DisciplinaId, disciplineName, q.Assunto, q.Enunciado,
            alternatives, IndexOfCorrect(alternatives, q.AlternativaCorreta), q.Explicacao, q.Dificuldade);
    }

    /// <summary>Disciplinas em que a pessoa pode criar questões: as dela (professor) ou todas (gestão).</summary>
    public async Task<List<DisciplineOption>> GetMyDisciplinesAsync()
    {
        var me = await session.RequireAsync(Teaching);
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = me.Id;
        var query = db.Disciplinas.AsNoTracking().Where(d => d.Ativa);
        if (!IsStaff(me))
            query = query.Where(d => d.ProfessorId == id);

        return await query.OrderBy(d => d.Nome).Select(d => new DisciplineOption(d.Id, d.Nome)).ToListAsync();
    }

    public async Task<List<QuestionRow>> GetMyQuestionsAsync(Guid? disciplineId, string? topic, DificuldadeQuestao? difficulty)
    {
        var me = await session.RequireAsync(Teaching);
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = me.Id;

        var query = db.Questoes.AsNoTracking().Where(q => q.Ativa);
        if (!IsStaff(me))
            query = query.Where(q => q.ProfessorId == id);
        if (disciplineId.HasValue)
            query = query.Where(q => q.DisciplinaId == disciplineId.Value);
        if (!string.IsNullOrWhiteSpace(topic))
        {
            var term = topic.Trim();
            query = query.Where(q => q.Assunto.Contains(term));
        }
        if (difficulty.HasValue)
            query = query.Where(q => q.Dificuldade == difficulty.Value);

        var rows = await query.OrderBy(q => q.Assunto).Take(300).ToListAsync();

        var names = (await db.Disciplinas.AsNoTracking().Select(d => new { d.Id, d.Nome }).ToListAsync())
            .ToDictionary(d => d.Id, d => d.Nome);

        return rows.Select(q => ToRow(q, names.TryGetValue(q.DisciplinaId, out var n) ? n : "—")).ToList();
    }

    /// <summary>
    /// Pede questões à IA e devolve RASCUNHOS (nada é salvo). A pessoa revisa e escolhe quais salvar.
    /// "recommendations" são orientações livres (ex.: "use casos práticos") que entram no prompt.
    /// Pode lançar <see cref="Application.Services.IaQuestaoException"/> (cota, chave ausente, falha da IA).
    /// </summary>
    public async Task<List<DraftQuestion>> GenerateAsync(
        Guid disciplineId, string topic, DificuldadeQuestao difficulty, int quantity, string? recommendations = null)
    {
        var me = await session.RequireAsync(Teaching);
        topic = (topic ?? "").Trim();
        if (topic.Length is < 2 or > 100)
            throw new ArgumentException("Informe um assunto entre 2 e 100 caracteres.");
        if ((recommendations ?? "").Length > 500)
            throw new ArgumentException("As recomendações devem ter até 500 caracteres.");
        quantity = Math.Clamp(quantity, 1, 10);

        await using var db = await dbFactory.CreateDbContextAsync();
        var myId = me.Id;
        var staff = IsStaff(me);
        var discipline = await db.Disciplinas.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == disciplineId && d.Ativa && (staff || d.ProfessorId == myId))
            ?? throw new InvalidOperationException("Disciplina não encontrada.");

        // Enunciados que já existem nesse assunto: a IA é instruída a não repeti-los.
        var existing = await db.Questoes.AsNoTracking()
            .Where(q => q.Ativa && q.DisciplinaId == disciplineId && q.Assunto == topic)
            .Take(15).Select(q => q.Enunciado).ToListAsync();

        var generated = await ia.GerarQuestoesAsync(disciplineId, discipline.Nome, topic, difficulty, quantity, existing, recommendations);

        return generated.Select(g =>
        {
            var alternatives = new List<string> { g.alternativa1, g.alternativa2, g.alternativa3, g.alternativa4, g.alternativa5 };
            return new DraftQuestion
            {
                Statement = g.enunciado,
                Alternatives = alternatives,
                CorrectIndex = Math.Max(0, IndexOfCorrect(alternatives, g.alternativaCorreta)),
                Explanation = g.explicacao
            };
        }).ToList();
    }

    /// <summary>Salva rascunhos (da IA ou digitados à mão) como questões ativas e devolve os IDs criados.</summary>
    public async Task<List<Guid>> SaveDraftsAsync(Guid disciplineId, string topic, DificuldadeQuestao difficulty, IEnumerable<DraftQuestion> drafts)
    {
        var me = await session.RequireAsync(Teaching);
        topic = (topic ?? "").Trim();
        if (topic.Length is < 2 or > 100)
            throw new ArgumentException("Informe um assunto entre 2 e 100 caracteres.");

        var list = drafts.ToList();
        if (list.Count == 0)
            throw new ArgumentException("Selecione ao menos uma questão para salvar.");
        foreach (var draft in list)
            Validate(draft);

        await using var db = await dbFactory.CreateDbContextAsync();
        var myId = me.Id;
        var staff = IsStaff(me);
        if (!await db.Disciplinas.AnyAsync(d => d.Id == disciplineId && d.Ativa && (staff || d.ProfessorId == myId)))
            throw new InvalidOperationException("Disciplina não encontrada.");

        var created = new List<Guid>();
        foreach (var d in list)
        {
            var a = d.Alternatives.Select(x => x.Trim()).ToList();
            // A coluna "ProfessorId" guarda o AUTOR da questão (professor, gestor ou administrador).
            var model = new QuestaoModel(
                me.Id, disciplineId, d.Statement.Trim(), a[d.CorrectIndex],
                a[0], a[1], a[2], a[3], a[4],
                topic, d.Explanation.Trim(), difficulty);
            db.Questoes.Add(model);
            created.Add(model.Id);
        }

        await db.SaveChangesAsync();
        return created;
    }

    public async Task DisableQuestionAsync(Guid id)
    {
        var me = await session.RequireAsync(Teaching);
        await using var db = await dbFactory.CreateDbContextAsync();
        var myId = me.Id;
        var staff = IsStaff(me);
        var question = await db.Questoes.FirstOrDefaultAsync(q => q.Id == id && q.Ativa && (staff || q.ProfessorId == myId))
            ?? throw new InvalidOperationException("A questão não foi encontrada.");
        question.Disable();
        await db.SaveChangesAsync();
    }

    // ───────────────────────── Aluno ─────────────────────────

    /// <summary>Disciplinas que têm ao menos uma questão ativa (as que dá para praticar).</summary>
    public async Task<List<PracticeDiscipline>> GetPracticeDisciplinesAsync()
    {
        await session.RequireAsync(Learners);
        await using var db = await dbFactory.CreateDbContextAsync();

        // Só duas colunas por questão; a contagem por dificuldade é feita em memória.
        var counts = await db.Questoes.AsNoTracking().Where(q => q.Ativa)
            .Select(q => new { q.DisciplinaId, q.Dificuldade }).ToListAsync();
        if (counts.Count == 0)
            return new();

        var ids = counts.Select(c => c.DisciplinaId).Distinct().ToList();
        var disciplines = await db.Disciplinas.AsNoTracking()
            .Where(d => d.Ativa && ids.Contains(d.Id))
            .Select(d => new { d.Id, d.Nome, d.ProfessorId }).ToListAsync();
        var teachers = (await db.Users.AsNoTracking().Select(u => new { u.Id, u.Name }).ToListAsync())
            .ToDictionary(u => u.Id, u => u.Name);

        return disciplines.Select(d =>
        {
            var mine = counts.Where(c => c.DisciplinaId == d.Id).ToList();
            return new PracticeDiscipline(
                d.Id, d.Nome, teachers.TryGetValue(d.ProfessorId, out var t) ? t : "—",
                mine.Count,
                mine.Count(c => c.Dificuldade == DificuldadeQuestao.Facil),
                mine.Count(c => c.Dificuldade == DificuldadeQuestao.Media),
                mine.Count(c => c.Dificuldade == DificuldadeQuestao.Dificil));
        }).OrderBy(d => d.Name).ToList();
    }

    /// <summary>
    /// Sorteia questões para uma sessão de prática e embaralha as alternativas de cada uma.
    /// Como o painel é Blazor Server, a resposta correta fica só no servidor até o aluno responder
    /// (o navegador recebe apenas o HTML que a tela desenha).
    /// </summary>
    public async Task<List<PracticeQuestion>> BuildPracticeAsync(Guid disciplineId, DificuldadeQuestao? difficulty, int count)
    {
        await session.RequireAsync(Learners);
        count = Math.Clamp(count, 1, 20);

        await using var db = await dbFactory.CreateDbContextAsync();
        var query = db.Questoes.AsNoTracking().Where(q => q.Ativa && q.DisciplinaId == disciplineId);
        if (difficulty.HasValue)
            query = query.Where(q => q.Dificuldade == difficulty.Value);

        var rows = await query.ToListAsync();
        var picked = rows.OrderBy(_ => Random.Shared.Next()).Take(count).ToList();

        var result = new List<PracticeQuestion>();
        foreach (var q in picked)
        {
            var original = new List<string> { q.Alternativa1, q.Alternativa2, q.Alternativa3, q.Alternativa4, q.Alternativa5 };
            var correctText = original[Math.Max(0, IndexOfCorrect(original, q.AlternativaCorreta))];
            if (IndexOfCorrect(original, q.AlternativaCorreta) < 0)
                continue; // questão inconsistente no banco: não entra na prática

            var shuffled = original.OrderBy(_ => Random.Shared.Next()).ToList();
            result.Add(new PracticeQuestion(
                q.Id, q.Assunto, q.Enunciado, shuffled, shuffled.IndexOf(correctText),
                q.Explicacao, q.Dificuldade));
        }

        return result;
    }

    // ───────────────────────── Auxiliares ─────────────────────────

    private static int IndexOfCorrect(List<string> alternatives, string correct) =>
        alternatives.FindIndex(a => string.Equals(a.Trim(), correct.Trim(), StringComparison.OrdinalIgnoreCase));

    private static void Validate(DraftQuestion d)
    {
        if ((d.Statement ?? "").Trim().Length is < 10 or > 1000)
            throw new ArgumentException("O enunciado deve ter entre 10 e 1000 caracteres.");
        if (d.Alternatives is null || d.Alternatives.Count != 5)
            throw new ArgumentException("Cada questão precisa de exatamente 5 alternativas.");
        if (d.Alternatives.Any(a => string.IsNullOrWhiteSpace(a) || a.Trim().Length > 300))
            throw new ArgumentException("Preencha as 5 alternativas (até 300 caracteres cada).");
        if (d.Alternatives.Select(a => a.Trim().ToLowerInvariant()).Distinct().Count() != 5)
            throw new ArgumentException("As alternativas não podem ser repetidas.");
        if (d.CorrectIndex is < 0 or > 4)
            throw new ArgumentException("Marque qual alternativa é a correta.");
        if ((d.Explanation ?? "").Trim().Length is < 1 or > 1000)
            throw new ArgumentException("Escreva a explicação da resposta (até 1000 caracteres).");
    }
}
