using EduAssess.Application.DTOs.Selo;
using EduAssess.Application.Interfaces;
using EduAssess.Application.Services;
using EduAssess.Data;
using EduAssess.Models;
using EduAssess.Models.Global;
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
    public static QuestionRow ToRow(QuestaoModel q, string disciplineName, List<SeloDto>? seals = null, int examCount = 0, int appliedExamCount = 0)
    {
        var alternatives = new List<string> { q.Alternativa1, q.Alternativa2, q.Alternativa3, q.Alternativa4, q.Alternativa5 };
        return new QuestionRow(
            q.Id, q.DisciplinaId, disciplineName, q.Assunto, q.Enunciado,
            alternatives, IndexOfCorrect(alternatives, q.AlternativaCorreta), q.Explicacao, q.Dificuldade,
            seals ?? new(), q.ProfessorId, examCount, appliedExamCount);
    }

    /// <summary>NOVO: catálogo de tipos de selo (o formulário se monta a partir dele).</summary>
    public async Task<List<SeloTipoDto>> GetSealTypesAsync()
    {
        await session.RequireAsync(Teaching);
        await using var db = await dbFactory.CreateDbContextAsync();
        return await QuestaoSeloService.ListarTiposAsync(db);
    }

    /// <summary>Disciplinas em que a pessoa pode criar questões: as dela (professor) ou todas (gestão).</summary>
    public async Task<List<DisciplineOption>> GetMyDisciplinesAsync()
    {
        var me = await session.RequireAsync(Teaching);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = me.Id;
        var query = db.Disciplinas.AsNoTracking().Where(d => d.InstituicaoId == institutionId && d.Ativa);
        if (!IsStaff(me))
            query = query.Where(d => d.ProfessorId == id);

        return await query.OrderBy(d => d.Nome).Select(d => new DisciplineOption(d.Id, d.Nome)).ToListAsync();
    }

    public async Task<List<QuestionRow>> GetMyQuestionsAsync(Guid? disciplineId, string? topic, DificuldadeQuestao? difficulty, string? sealCode = null)
    {
        var me = await session.RequireAsync(Teaching);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = me.Id;

        var query = db.Questoes.AsNoTracking().Where(q => q.InstituicaoId == institutionId && q.Ativa);
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
        // NOVO: filtro por selo (ex.: só questões ENEM)
        if (!string.IsNullOrWhiteSpace(sealCode))
        {
            var code = sealCode.Trim().ToUpperInvariant();
            query = query.Where(q => q.Selos.Any(s => s.SeloTipo.Codigo == code));
        }

        var rows = await query.OrderBy(q => q.Assunto).Take(300).ToListAsync();

        var names = (await db.Disciplinas.AsNoTracking().Where(d => d.InstituicaoId == institutionId).Select(d => new { d.Id, d.Nome }).ToListAsync())
            .ToDictionary(d => d.Id, d => d.Nome);

        return await ToRowsAsync(db, rows, names);
    }

    /// <summary>Monta as linhas com os selos (todos, inclusive os ocultos, que o professor enxerga esmaecidos) e o uso em avaliações.</summary>
    private static async Task<List<QuestionRow>> ToRowsAsync(AppDbContext db, List<QuestaoModel> rows, Dictionary<Guid, string> names)
    {
        var ids = rows.Select(r => r.Id).ToList();
        var seals = await QuestaoSeloService.CarregarAsync(db, ids, onlyVisible: false);
        var usage = await db.AvaliacoesQuestoes.AsNoTracking()
            .Where(l => ids.Contains(l.QuestaoId) && l.Avaliacao.Ativa)
            .Select(l => new { l.QuestaoId, Applied = l.Avaliacao.Status == StatusAvaliacao.Aplicada }).ToListAsync();
        return rows.Select(q => ToRow(q, names.TryGetValue(q.DisciplinaId, out var n) ? n : "—",
            seals.TryGetValue(q.Id, out var s) ? s : new(),
            usage.Count(u => u.QuestaoId == q.Id), usage.Count(u => u.QuestaoId == q.Id && u.Applied))).ToList();
    }

    /// <summary>NOVO: uma questão para edição (com todos os selos). Respeita a posse: professor só as próprias.</summary>
    public async Task<QuestionRow> GetQuestionAsync(Guid id)
    {
        var me = await session.RequireAsync(Teaching);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var myId = me.Id; var staff = IsStaff(me);
        var q = await db.Questoes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.InstituicaoId == institutionId && x.Ativa && (staff || x.ProfessorId == myId))
            ?? throw new InvalidOperationException("A questão não foi encontrada ou você não pode editá-la.");
        var names = (await db.Disciplinas.AsNoTracking().Where(d => d.InstituicaoId == institutionId).Select(d => new { d.Id, d.Nome }).ToListAsync())
            .ToDictionary(d => d.Id, d => d.Nome);
        return (await ToRowsAsync(db, [q], names)).First();
    }

    /// <summary>
    /// NOVO: edição COMPLETA de uma questão já cadastrada: enunciado, alternativas, gabarito, justificativa,
    /// assunto, dificuldade, disciplina e selos. Mesmas regras de posse e de validação do cadastro.
    /// Resultados de simulados já gravados não mudam (guardam só a nota do aluno).
    /// </summary>
    public async Task UpdateQuestionAsync(Guid id, QuestionEditForm form)
    {
        var me = await session.RequireAsync(Teaching);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        var topic = (form.Topic ?? "").Trim();
        if (topic.Length is < 2 or > 100)
            throw new ArgumentException("Informe um assunto entre 2 e 100 caracteres.");
        Validate(form.Content);

        await using var db = await dbFactory.CreateDbContextAsync();
        var myId = me.Id; var staff = IsStaff(me);
        var question = await db.Questoes.FirstOrDefaultAsync(q => q.Id == id && q.InstituicaoId == institutionId && q.Ativa && (staff || q.ProfessorId == myId))
            ?? throw new InvalidOperationException("A questão não foi encontrada ou você não pode editá-la.");
        if (!await db.Disciplinas.AnyAsync(d => d.Id == form.DisciplineId && d.InstituicaoId == institutionId && d.Ativa && (staff || d.ProfessorId == myId)))
            throw new InvalidOperationException("Disciplina não encontrada.");

        var a = form.Content.Alternatives.Select(x => x.Trim()).ToList();
        question.UpdateQuestion(form.DisciplineId, form.Content.Statement.Trim(), a[form.Content.CorrectIndex],
            a[0], a[1], a[2], a[3], a[4], topic, form.Content.Explanation.Trim(), form.Difficulty);
        await QuestaoSeloService.SincronizarAsync(db, id, ToInputs(form.Seals));
        await db.SaveChangesAsync();
    }

    public static List<SeloInput> ToInputs(IEnumerable<SealChoice>? choices) =>
        (choices ?? []).Where(c => c.Selected).Select(c => new SeloInput(c.Code, c.Year, c.Detail, c.Show)).ToList();

    /// <summary>
    /// Pede questões à IA e devolve RASCUNHOS (nada é salvo). A pessoa revisa e escolhe quais salvar.
    /// "recommendations" são orientações livres (ex.: "use casos práticos") que entram no prompt.
    /// Pode lançar <see cref="Application.Services.IaQuestaoException"/> (cota, chave ausente, falha da IA).
    /// </summary>
    public async Task<List<DraftQuestion>> GenerateAsync(
        Guid disciplineId, string topic, DificuldadeQuestao difficulty, int quantity, string? recommendations = null)
    {
        var me = await session.RequireAsync(Teaching);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
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
            .FirstOrDefaultAsync(d => d.Id == disciplineId && d.InstituicaoId == institutionId && d.Ativa && (staff || d.ProfessorId == myId))
            ?? throw new InvalidOperationException("Disciplina não encontrada.");

        // Enunciados que já existem nesse assunto: a IA é instruída a não repeti-los.
        var existing = await db.Questoes.AsNoTracking()
            .Where(q => q.InstituicaoId == institutionId && q.Ativa && q.DisciplinaId == disciplineId && q.Assunto == topic)
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
    public async Task<List<Guid>> SaveDraftsAsync(Guid disciplineId, string topic, DificuldadeQuestao difficulty, IEnumerable<DraftQuestion> drafts, IEnumerable<SeloInput>? seals = null)
    {
        var me = await session.RequireAsync(Teaching);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
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
        if (!await db.Disciplinas.AnyAsync(d => d.Id == disciplineId && d.InstituicaoId == institutionId && d.Ativa && (staff || d.ProfessorId == myId)))
            throw new InvalidOperationException("Disciplina não encontrada.");

        var sealList = seals?.ToList() ?? new();
        await QuestaoSeloService.ValidarAsync(db, sealList);   // falha cedo, antes de salvar qualquer questão

        var created = new List<Guid>();
        foreach (var d in list)
        {
            var a = d.Alternatives.Select(x => x.Trim()).ToList();
            // A coluna "ProfessorId" guarda o AUTOR da questão (professor, gestor ou administrador).
            var model = new QuestaoModel(
                me.Id, disciplineId, d.Statement.Trim(), a[d.CorrectIndex],
                a[0], a[1], a[2], a[3], a[4],
                topic, d.Explanation.Trim(), difficulty, institutionId);
            db.Questoes.Add(model);
            created.Add(model.Id);
            await QuestaoSeloService.SincronizarAsync(db, model.Id, sealList);
        }

        await db.SaveChangesAsync();
        return created;
    }

    public async Task DisableQuestionAsync(Guid id)
    {
        var me = await session.RequireAsync(Teaching);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var myId = me.Id;
        var staff = IsStaff(me);
        var question = await db.Questoes.FirstOrDefaultAsync(q => q.Id == id && q.InstituicaoId == institutionId && q.Ativa && (staff || q.ProfessorId == myId))
            ?? throw new InvalidOperationException("A questão não foi encontrada.");
        question.Disable();
        await db.SaveChangesAsync();
    }

    // ───────────────────────── Aluno ─────────────────────────

    /// <summary>Disciplinas que têm ao menos uma questão ativa (as que dá para praticar).</summary>
    public async Task<List<PracticeDiscipline>> GetPracticeDisciplinesAsync()
    {
        await session.RequireAsync(Learners);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();

        // Só duas colunas por questão; a contagem por dificuldade é feita em memória.
        var counts = await db.Questoes.AsNoTracking().Where(q => q.InstituicaoId == institutionId && q.Ativa)
            .Select(q => new { q.DisciplinaId, q.Dificuldade }).ToListAsync();
        if (counts.Count == 0)
            return new();

        var ids = counts.Select(c => c.DisciplinaId).Distinct().ToList();
        var disciplines = await db.Disciplinas.AsNoTracking()
            .Where(d => d.InstituicaoId == institutionId && d.Ativa && ids.Contains(d.Id))
            .Select(d => new { d.Id, d.Nome, d.ProfessorId }).ToListAsync();
        var teachers = (await db.Users.AsNoTracking()
            .Where(u => db.UsuariosInstituicoes.Any(ui => ui.UsuarioId == u.Id && ui.InstituicaoId == institutionId && ui.Ativa))
            .Select(u => new { u.Id, u.Name }).ToListAsync())
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
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        count = Math.Clamp(count, 1, 20);

        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.Disciplinas.AnyAsync(d => d.Id == disciplineId && d.InstituicaoId == institutionId && d.Ativa))
            throw new InvalidOperationException("Disciplina não encontrada.");
        var query = db.Questoes.AsNoTracking().Where(q => q.InstituicaoId == institutionId && q.Ativa && q.DisciplinaId == disciplineId);
        if (difficulty.HasValue)
            query = query.Where(q => q.Dificuldade == difficulty.Value);

        var rows = await query.ToListAsync();
        var picked = rows.OrderBy(_ => Random.Shared.Next()).Take(count).ToList();
        // NOVO: o aluno só recebe os selos que o professor escolheu exibir (filtrado aqui, no servidor).
        var visibleSeals = await QuestaoSeloService.CarregarAsync(db, picked.Select(p => p.Id), onlyVisible: true);

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
                q.Explicacao, q.Dificuldade, visibleSeals.TryGetValue(q.Id, out var sl) ? sl : new()));
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
