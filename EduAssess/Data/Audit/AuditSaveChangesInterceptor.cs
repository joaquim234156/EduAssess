using EduAssess.Models;
using EduAssess.Models.Global;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EduAssess.Data.Audit;

/// <summary>
/// Auditoria automática: a cada SaveChanges, grava um RegistroAuditoria por entidade criada, editada,
/// excluída ou com status alterado. Cobre o painel Blazor e a API REST com o mesmo código.
/// Também é a trava de integridade: recusa qualquer alteração/exclusão de um RegistroAuditoria.
/// Nunca grava VALORES de campos (só os NOMES dos campos alterados), exceto status (ativo, perfil, papel),
/// para não vazar CPF, senha etc. para um histórico que não pode ser apagado.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    public static readonly AuditSaveChangesInterceptor Instance = new();

    private static readonly HashSet<string> StatusProps = new() { "Ativa", "Ativo", "Status", "Role", "Papel", "Contabilizar" };
    private static readonly HashSet<string> IgnoredProps = new() { "AtualizadaEm", "AtualizadoEm", "DesvinculoEm", "VinculadoEm" };

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context, null).GetAwaiter().GetResult();
        return result;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        await Apply(eventData.Context, cancellationToken);
        return result;
    }

    private sealed record Pending(EntityEntry Entry, AcaoAuditoria Acao, string Detalhes);

    private static async Task Apply(DbContext? ctx, CancellationToken? ct)
    {
        if (ctx is not AppDbContext db) return;
        var entries = db.ChangeTracker.Entries().ToList();

        // 1) Registros de auditoria são só de inserção.
        if (entries.Any(e => e.Entity is RegistroAuditoriaModel && e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Registros de auditoria são somente leitura: não podem ser alterados nem excluídos.");

        if (db.SuppressAudit) return;

        var pending = new List<Pending>();
        foreach (var e in entries)
        {
            if (e.Entity is RegistroAuditoriaModel or ContatoModel) continue;
            switch (e.State)
            {
                case EntityState.Added:
                    pending.Add(new Pending(e, AcaoAuditoria.Criacao, ""));
                    break;
                case EntityState.Deleted:
                    pending.Add(new Pending(e, AcaoAuditoria.Exclusao, ""));
                    break;
                case EntityState.Modified:
                    var changed = e.Properties.Where(p => p.IsModified && !IgnoredProps.Contains(p.Metadata.Name)).ToList();
                    if (changed.Count == 0) break;
                    var status = changed.Where(p => StatusProps.Contains(p.Metadata.Name)).ToList();
                    var others = changed.Where(p => !StatusProps.Contains(p.Metadata.Name)).Select(p => Friendly(p.Metadata.Name)).Distinct().ToList();
                    if (e.Entity is UserModel && changed.Any(p => p.Metadata.Name == "PasswordHash"))
                    {
                        others = others.Where(n => n != "PasswordHash").ToList();
                        others.Add("senha");
                    }
                    var parts = status.Select(p => $"{Friendly(p.Metadata.Name)}: {Show(p.OriginalValue)} → {Show(p.CurrentValue)}").ToList();
                    if (others.Count > 0) parts.Add("campos alterados: " + string.Join(", ", others));
                    pending.Add(new Pending(e, status.Count > 0 ? AcaoAuditoria.StatusAlterado : AcaoAuditoria.Edicao, string.Join(" · ", parts)));
                    break;
            }
        }
        if (pending.Count == 0) return;

        var info = await Lookups.LoadAsync(db, pending.Select(p => p.Entry.Entity).ToList(), ct);
        var actor = db.CurrentActor;
        // Na API o token traz o e-mail como "nome": troca pelo nome da pessoa para o histórico ficar legível.
        var actorName = actor?.Name;
        if (actor?.UserId is Guid actorId && (actorName is null || actorName.Contains('@')))
            actorName = info.Users.TryGetValue(actorId, out var known) ? known
                : await db.Users.AsNoTracking().Where(u => u.Id == actorId).Select(u => u.Name).FirstOrDefaultAsync(ct ?? CancellationToken.None) ?? actorName;
        foreach (var p in pending)
        {
            var d = Describe(p.Entry.Entity, info);
            if (d is null) continue;
            var record = new RegistroAuditoriaModel(p.Acao, d.Categoria, d.Entidade,
                actor?.UserId, actorName ?? "Sistema", actor?.Role ?? "", d.InstituicaoId ?? actor?.InstitutionId)
                .Com(d.EntidadeId, d.Rotulo, string.IsNullOrEmpty(p.Detalhes) ? d.Detalhes : p.Detalhes, d.ProfessorId, d.AlunoId, actor?.Ip);
            db.RegistrosAuditoria.Add(record);
        }
    }

    // Nomes legíveis para o histórico (os nomes técnicos das colunas não dizem nada a quem lê).
    private static string Friendly(string name) => name switch
    {
        "Enunciado" => "enunciado", "AlternativaCorreta" => "gabarito", "Explicacao" => "justificativa",
        "Alternativa1" or "Alternativa2" or "Alternativa3" or "Alternativa4" or "Alternativa5" => "alternativas",
        "Assunto" => "assunto", "Dificuldade" => "dificuldade", "DisciplinaId" => "disciplina",
        "Ativa" or "Ativo" => "ativo", "Role" => "perfil", "Papel" => "papel", "Nome" or "Name" => "nome",
        "CorPrimaria" => "cor primária", "CorSecundaria" => "cor secundária", "LogoPath" => "logotipo",
        "Endereco" => "endereço", "Telefone" => "telefone", "Email" => "e-mail", "Cnpj" => "CNPJ", "Sigla" => "sigla",
        "Tipo" => "categoria", "MantenedorId" => "mantenedor", "UnidadeId" => "unidade", "Titulo" => "título",
        "Descricao" => "descrição", "Valor" => "valor", "Codigo" => "código",
        _ => name
    };

    private static string Show(object? v) => v switch
    {
        null => "—", true => "sim", false => "não", _ => v.ToString() ?? "—"
    };

    // ───────────── descrição de cada entidade ─────────────

    private sealed record Described(CategoriaAuditoria Categoria, string Entidade, string? EntidadeId, string? Rotulo,
        Guid? InstituicaoId, Guid? ProfessorId, Guid? AlunoId, string? Detalhes = null);

    private static string Key(EntityEntry e) => string.Join("/", e.Metadata.FindPrimaryKey()!.Properties.Select(p => e.Property(p.Name).CurrentValue));

    private static Described? Describe(object entity, Lookups l)
    {
        switch (entity)
        {
            case UserModel u: return new(CategoriaAuditoria.Gestao, "Usuario", u.Id.ToString(), u.Name, null, null, null);
            case CursoModel c: return new(CategoriaAuditoria.Gestao, "Curso", c.Id.ToString(), c.Nome, c.InstituicaoId, null, null);
            case DisciplinaModel d: return new(CategoriaAuditoria.Gestao, "Disciplina", d.Id.ToString(), d.Nome, d.InstituicaoId, d.ProfessorId, null);
            case TurmaModel t: return new(CategoriaAuditoria.Gestao, "Turma", t.Id.ToString(), t.Nome, t.InstituicaoId, t.ProfessorId, null);
            case MatriculaModel m:
                l.Turmas.TryGetValue(m.TurmaId, out var tm);
                return new(CategoriaAuditoria.Gestao, "Matricula", m.Id.ToString(),
                    $"{l.UserName(m.UsuarioId)} · {tm?.Nome ?? "turma"}", m.InstituicaoId, tm?.ProfessorId, m.UsuarioId);
            case NotaModel n:
                l.Matriculas.TryGetValue(n.MatriculaId, out var mt);
                Guid? prof = null; Guid? aluno = null; var turma = "";
                if (mt is not null) { aluno = mt.UsuarioId; if (l.Turmas.TryGetValue(mt.TurmaId, out var tt)) { prof = tt.ProfessorId; turma = tt.Nome; } }
                return new(CategoriaAuditoria.Gestao, "Nota", n.Id.ToString(),
                    $"{n.Descricao} · {n.Valor:0.0#} · {l.UserName(aluno)}{(turma == "" ? "" : " · " + turma)}", n.InstituicaoId, prof, aluno);
            case InstituicaoModel i: return new(CategoriaAuditoria.Gestao, "Instituicao", i.Id.ToString(), i.Nome, i.Id, null, null);
            case UnidadeModel un: return new(CategoriaAuditoria.Gestao, "Unidade", un.Id.ToString(), un.Nome, un.InstituicaoId, null, null);
            case MantenedorModel mn: return new(CategoriaAuditoria.Gestao, "Mantenedor", mn.Id.ToString(), mn.Nome, null, null, null);
            case UsuarioInstituicaoModel ui:
                return new(CategoriaAuditoria.Gestao, "Vinculo", $"{ui.UsuarioId}/{ui.InstituicaoId}", l.UserName(ui.UsuarioId),
                    ui.InstituicaoId, null, null, $"papel {ui.Papel}");
            case QuestaoModel q:
                return new(CategoriaAuditoria.Conteudo, "Questao", q.Id.ToString(), $"{q.Assunto}: {Short(q.Enunciado)}", q.InstituicaoId, q.ProfessorId, null);
            case QuestaoSeloModel qs:
                l.Questoes.TryGetValue(qs.QuestaoId, out var qq);
                return new(CategoriaAuditoria.Conteudo, "Selo", qs.QuestaoId.ToString(),
                    $"{(l.Selos.TryGetValue(qs.SeloTipoId, out var sn) ? sn : "selo")}{(qs.Ano is null ? "" : " " + qs.Ano)}{(string.IsNullOrEmpty(qs.Detalhe) ? "" : " " + qs.Detalhe)}",
                    qq?.InstituicaoId, qq?.ProfessorId, null, qq is null ? null : $"questão: {qq.Assunto}");
            case AvaliacaoModel a:
                return new(CategoriaAuditoria.Conteudo, "Avaliacao", a.Id.ToString(), $"{a.Codigo} · {a.Titulo}", a.InstituicaoId, a.AutorId, null);
            case AvaliacaoQuestaoModel aq:
                l.Avaliacoes.TryGetValue(aq.AvaliacaoId, out var av);
                return new(CategoriaAuditoria.Conteudo, "AvaliacaoQuestao", aq.AvaliacaoId.ToString(), av is null ? "avaliação" : $"{av.Codigo} · {av.Titulo}",
                    av?.InstituicaoId, av?.AutorId, null, "composição da avaliação");
            case ResultadoPraticaModel r:
                return new(CategoriaAuditoria.Desempenho, "ResultadoPratica", r.Id.ToString(), $"{r.Titulo} ({r.Acertos}/{r.TotalQuestoes})", r.InstituicaoId, null, r.UsuarioId);
            case SeloTipoModel st: return new(CategoriaAuditoria.Sistema, "SeloTipo", st.Id.ToString(), st.Nome, null, null, null);
            default: return null;   // entidades sem interesse de auditoria
        }
    }

    private static string Short(string? v) => string.IsNullOrEmpty(v) ? "" : v.Length <= 60 ? v : v[..60] + "…";

    private sealed record TurmaInfo(string Nome, Guid ProfessorId);
    private sealed record MatInfo(Guid UsuarioId, Guid TurmaId);
    private sealed record AvInfo(string Codigo, string Titulo, Guid AutorId, Guid? InstituicaoId);
    private sealed record QInfo(string Assunto, Guid ProfessorId, Guid? InstituicaoId);

    /// <summary>Consultas pequenas (em lote) para descobrir professor dono, aluno e nomes legíveis.</summary>
    private sealed class Lookups
    {
        public Dictionary<Guid, TurmaInfo> Turmas { get; } = new();
        public Dictionary<Guid, MatInfo> Matriculas { get; } = new();
        public Dictionary<Guid, AvInfo> Avaliacoes { get; } = new();
        public Dictionary<Guid, QInfo> Questoes { get; } = new();
        public Dictionary<Guid, string> Selos { get; } = new();
        public Dictionary<Guid, string> Users { get; } = new();
        public string UserName(Guid? id) => id is Guid g && Users.TryGetValue(g, out var n) ? n : "—";

        public static async Task<Lookups> LoadAsync(AppDbContext db, List<object> entities, CancellationToken? ct)
        {
            var l = new Lookups();
            var token = ct ?? CancellationToken.None;
            var matriculaIds = entities.OfType<NotaModel>().Select(n => n.MatriculaId).Distinct().ToList();
            var userIds = new HashSet<Guid>(entities.OfType<MatriculaModel>().Select(m => m.UsuarioId));
            foreach (var ui in entities.OfType<UsuarioInstituicaoModel>()) userIds.Add(ui.UsuarioId);
            var turmaIds = new HashSet<Guid>(entities.OfType<MatriculaModel>().Select(m => m.TurmaId));

            if (matriculaIds.Count > 0)
                foreach (var m in await db.Matriculas.AsNoTracking().Where(m => matriculaIds.Contains(m.Id))
                    .Select(m => new { m.Id, m.UsuarioId, m.TurmaId }).ToListAsync(token))
                { l.Matriculas[m.Id] = new MatInfo(m.UsuarioId, m.TurmaId); userIds.Add(m.UsuarioId); turmaIds.Add(m.TurmaId); }

            if (turmaIds.Count > 0)
                foreach (var t in await db.Turmas.AsNoTracking().Where(t => turmaIds.Contains(t.Id))
                    .Select(t => new { t.Id, t.Nome, t.ProfessorId }).ToListAsync(token))
                    l.Turmas[t.Id] = new TurmaInfo(t.Nome, t.ProfessorId);

            var avIds = entities.OfType<AvaliacaoQuestaoModel>().Select(a => a.AvaliacaoId).Distinct().ToList();
            if (avIds.Count > 0)
                foreach (var a in await db.Avaliacoes.AsNoTracking().Where(a => avIds.Contains(a.Id))
                    .Select(a => new { a.Id, a.Codigo, a.Titulo, a.AutorId, a.InstituicaoId }).ToListAsync(token))
                    l.Avaliacoes[a.Id] = new AvInfo(a.Codigo, a.Titulo, a.AutorId, a.InstituicaoId);

            var qIds = entities.OfType<QuestaoSeloModel>().Select(s => s.QuestaoId).Distinct().ToList();
            if (qIds.Count > 0)
                foreach (var q in await db.Questoes.AsNoTracking().Where(q => qIds.Contains(q.Id))
                    .Select(q => new { q.Id, q.Assunto, q.ProfessorId, q.InstituicaoId }).ToListAsync(token))
                    l.Questoes[q.Id] = new QInfo(q.Assunto, q.ProfessorId, q.InstituicaoId);

            var sIds = entities.OfType<QuestaoSeloModel>().Select(s => s.SeloTipoId).Distinct().ToList();
            if (sIds.Count > 0)
                foreach (var s in await db.SelosTipos.AsNoTracking().Where(s => sIds.Contains(s.Id)).Select(s => new { s.Id, s.Nome }).ToListAsync(token))
                    l.Selos[s.Id] = s.Nome;

            if (userIds.Count > 0)
                foreach (var u in await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).Select(u => new { u.Id, u.Name }).ToListAsync(token))
                    l.Users[u.Id] = u.Name;
            return l;
        }
    }
}
