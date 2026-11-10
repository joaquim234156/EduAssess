using System.Security.Claims;
using EduAssess.Data;
using EduAssess.Models;
using EduAssess.Models.Local;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Web;

/// <summary>
/// Sessão do painel. A identidade vem do cookie de autenticação (sobrevive a F5 e a novas abas);
/// cada operação protegida revalida no banco se a conta continua ativa e qual é o perfil atual.
/// </summary>
public sealed class UiSession(AuthenticationStateProvider authenticationStateProvider, IDbContextFactory<AppDbContext> dbFactory)
{
    private bool initialized;

    /// <summary>
    /// Avisa o layout (menu lateral e topo) quando algo da sessão muda sem trocar de página,
    /// por exemplo a foto de perfil. Assim o avatar novo aparece na hora.
    /// </summary>
    public event Action? Changed;

    /// <summary>Muda a cada troca/remoção de foto; usado para o navegador baixar a imagem nova.</summary>
    public long AvatarStamp { get; private set; }

    public void NotifyAvatarChanged()
    {
        AvatarStamp = DateTime.UtcNow.Ticks;
        Changed?.Invoke();
    }

    // ───────── NOVO: unidade (prédio) e tema ─────────

    /// <summary>Unidade (prédio) em que a pessoa está lotada. Nula = sem unidade definida (visual da rede).</summary>
    public UnitInfo? Unit { get; private set; }

    /// <summary>Nome da instituição da pessoa (ex.: Faculdade Luciano Feijão), mesmo sem unidade.</summary>
    public string InstitutionName { get; private set; } = "Faculdade Luciano Feijão";

    /// <summary>Todas as unidades ativas (só carregadas para o Admin, que pode "ver como" cada prédio).</summary>
    public IReadOnlyList<UnitInfo> AllUnits { get; private set; } = Array.Empty<UnitInfo>();

    /// <summary>
    /// NOVO: tema por CURSO. Vale "medicina" quando a pessoa é aluno com matrícula ativa, ou professor com turma
    /// ativa, no curso de Medicina. Nulo para os demais. Tem prioridade sobre o tema do prédio.
    /// </summary>
    public string? CourseThemeKey { get; private set; }

    /// <summary>Escolha do Admin para "ver como" (vale só nesta sessão): "" = não mexer, "unit:&lt;id&gt;" ou "course:medicina".</summary>
    public string ThemeChoice { get; private set; } = "";

    /// <summary>Unidade escolhida pelo Admin para visualizar o tema de outro prédio.</summary>
    public UnitInfo? ThemeOverride =>
        ThemeChoice.StartsWith("unit:") && Guid.TryParse(ThemeChoice[5..], out var id) ? AllUnits.FirstOrDefault(u => u.Id == id) : null;

    /// <summary>O Admin escolheu visualizar o tema do curso de Medicina?</summary>
    public bool ThemeOverrideCourse => ThemeChoice == "course:" + UiTheme.MedicineKey;

    /// <summary>Prédio considerado agora (o escolhido pelo Admin ou o da própria pessoa).</summary>
    public UnitInfo? ThemeUnit => ThemeOverride ?? Unit;

    /// <summary>Chave do tema ativo: curso de Medicina > prédio. Ex.: "medicina", "anexo", "odontologia", "principal", "rede".</summary>
    public string ThemeKey =>
        ThemeOverrideCourse ? UiTheme.MedicineKey
        : ThemeOverride is not null ? UiTheme.UnitKey(ThemeOverride.Slug)
        : CourseThemeKey ?? UiTheme.UnitKey(Unit?.Slug);

    /// <summary>Texto do selo na barra lateral e no cabeçalho: "Curso de Medicina · Anexo", "Prédio Principal"...</summary>
    public string ThemeLabel => ThemeKey == UiTheme.MedicineKey
        ? "Curso de Medicina" + (ThemeUnit is null ? "" : " · " + ThemeUnit.Name)
        : ThemeUnit?.Name ?? InstitutionName;

    /// <summary>Sigla curta para a etiqueta do topo (MED para o curso de Medicina, ou a sigla do prédio).</summary>
    public string ThemeShort => ThemeKey == UiTheme.MedicineKey ? "MED" : ThemeUnit?.Sigla ?? "";

    /// <summary>Classes CSS do elemento raiz do painel (perfil + tema).</summary>
    public string ThemeClass => $"role-{UiTheme.RoleKey(User?.Role)} unit-{ThemeKey}";

    public string? ThemeStyle => ThemeKey == "custom" ? UiTheme.CustomStyle(ThemeUnit) : null;

    /// <summary>Admin escolhe "ver como" um prédio ou o perfil de Medicina (ou volta à visão da rede com vazio).</summary>
    public void SetThemeChoice(string? choice)
    {
        if (User?.Role != UserRole.Admin) return;
        ThemeChoice = choice ?? "";
        Changed?.Invoke();
    }

    /// <summary>Recarrega unidade e tema por curso (usado depois de mudar a lotação de alguém na tela Instituição).</summary>
    public async Task RefreshUnitAsync()
    {
        if (User is null) return;
        await LoadUnitAsync(User.Id);
        Changed?.Invoke();
    }

    public UserSummary? User { get; private set; }
    public bool IsSignedIn => User is not null;
    public bool CanManage => User?.Role is UserRole.Admin or UserRole.Gestor;
    public bool CanCreateManager => User?.Role == UserRole.Admin;
    public bool CanCreateProfessor => User?.Role == UserRole.Gestor;
    public bool CanAssignStudents => User?.Role is UserRole.Admin or UserRole.Gestor or UserRole.Professor;
    public bool CanGrade => CanAssignStudents;
    public bool IsProfessor => User?.Role == UserRole.Professor;

    /// <summary>Carrega a identidade do cookie. Deve ser chamado no início das páginas.</summary>
    public async Task InitializeAsync()
    {
        if (initialized)
            return;

        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        var principal = state.User;

        if (principal.Identity?.IsAuthenticated == true
            && Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
            && Enum.TryParse<UserRole>(principal.FindFirst(ClaimTypes.Role)?.Value, out var role))
        {
            User = new UserSummary(
                id,
                principal.FindFirst(ClaimTypes.Name)?.Value ?? "Usuário",
                principal.FindFirst(ClaimTypes.Email)?.Value ?? "",
                role);
        }

        if (User is not null)
            await LoadUnitAsync(User.Id);

        initialized = true;
    }

    /// <summary>
    /// NOVO: busca a unidade (prédio) da pessoa e, para o Admin, a lista de todas as unidades.
    /// Se a tabela ainda não existir ou der erro, o painel simplesmente usa o visual da rede.
    /// </summary>
    private async Task LoadUnitAsync(Guid userId)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();

            var link = await db.UsuariosInstituicoes.AsNoTracking()
                .Where(ui => ui.UsuarioId == userId && ui.Ativa)
                .OrderByDescending(ui => ui.VinculadoEm)
                .Select(ui => new
                {
                    ui.InstituicaoId,
                    Institution = ui.Instituicao!.Nome,
                    Unit = ui.Unidade == null ? null : new { ui.Unidade.Id, ui.Unidade.Nome, ui.Unidade.Slug, ui.Unidade.Sigla, ui.Unidade.Tipo, ui.Unidade.Ativa }
                })
                .FirstOrDefaultAsync();

            Unit = link?.Unit is { Ativa: true } u
                ? new UnitInfo(u.Id, u.Nome, u.Slug, u.Sigla ?? "", u.Tipo, link.InstituicaoId, link.Institution)
                : null;
            InstitutionName = link?.Institution ?? InstitutionName;

            CourseThemeKey = await DetectCourseThemeAsync(db, userId);

            if (User?.Role == UserRole.Admin)
            {
                AllUnits = await db.Unidades.AsNoTracking()
                    .Where(x => x.Ativa)
                    .OrderBy(x => x.Nome)
                    .Select(x => new UnitInfo(x.Id, x.Nome, x.Slug, x.Sigla ?? "", x.Tipo, x.InstituicaoId, x.Instituicao!.Nome))
                    .ToListAsync();
            }
        }
        catch (Exception)
        {
            // Mantém o visual padrão da rede: o tema nunca pode impedir o painel de abrir.
            Unit = null;
        }
    }

    /// <summary>
    /// Revalida a conta no banco e garante que o perfil atual está entre os permitidos.
    /// Lança <see cref="UnauthorizedAccessException"/> quando a sessão não é mais válida.
    /// </summary>
    public async Task<UserSummary> RequireAsync(params UserRole[] allowedRoles)
    {
        await InitializeAsync();
        if (User is null)
            throw new UnauthorizedAccessException("Faça login para continuar.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var userId = User.Id;
        var row = await db.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new { x.Name, x.Email, x.Role, x.Ativa })
            .FirstOrDefaultAsync();

        if (row is null || !row.Ativa)
        {
            User = null;
            throw new UnauthorizedAccessException("Sua sessão expirou. Entre novamente.");
        }

        User = new UserSummary(userId, row.Name, row.Email, row.Role);

        if (allowedRoles.Length > 0 && !allowedRoles.Contains(row.Role))
            throw new UnauthorizedAccessException("Seu perfil não tem permissão para esta ação.");

        return User;
    }

    /// <summary>
    /// NOVO: o visual de Medicina vem do CURSO, não do prédio. Aluno: matrícula ativa em turma do curso de
    /// Medicina. Professor: turma ativa do curso de Medicina. "Medicina Veterinária" não conta.
    /// Regra pelo nome do curso (começa com "Medicina"); se o nome mudar, ajuste aqui.
    /// </summary>
    private async Task<string?> DetectCourseThemeAsync(AppDbContext db, Guid userId)
    {
        var role = User?.Role;
        bool inMedicine = false;

        if (role == UserRole.Aluno)
        {
            inMedicine = await db.Matriculas.AsNoTracking().AnyAsync(m =>
                m.UsuarioId == userId && m.Ativa && m.Turma.Ativa && m.Turma.Curso.Ativo
                && EF.Functions.Like(m.Turma.Curso.Nome, "Medicina%")
                && !EF.Functions.Like(m.Turma.Curso.Nome, "%veterin%"));
        }
        else if (role == UserRole.Professor)
        {
            inMedicine = await db.Turmas.AsNoTracking().AnyAsync(t =>
                t.ProfessorId == userId && t.Ativa && t.Curso.Ativo
                && EF.Functions.Like(t.Curso.Nome, "Medicina%")
                && !EF.Functions.Like(t.Curso.Nome, "%veterin%"));
        }

        return inMedicine ? UiTheme.MedicineKey : null;
    }
}

// CORREÇÃO: este tipo ficava no fim do UiSession.cs e foi apagado por engano numa edição anterior,
// o que causava o erro "UserSummary não pode ser encontrado" (CS0246) em várias telas e serviços.
/// <summary>Resumo da pessoa logada (guardado na sessão do painel).</summary>
public sealed record UserSummary(Guid Id, string Name, string Email, UserRole Role);
