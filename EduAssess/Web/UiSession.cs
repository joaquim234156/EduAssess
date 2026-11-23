using System.Security.Claims;
using EduAssess.Data;
using EduAssess.Data.Audit;
using EduAssess.Models;
using EduAssess.Models.Global;
using EduAssess.Models.Local;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Web;

/// <summary>
/// Sessão do painel. A identidade vem do cookie de autenticação (sobrevive a F5 e a novas abas);
/// cada operação protegida revalida no banco se a conta continua ativa e qual é o perfil atual.
/// </summary>
public sealed class UiSession(AuthenticationStateProvider authenticationStateProvider, IDbContextFactory<AppDbContext> dbFactory,
    IAuditActorAccessor auditActor, ImageStorageService images)
{
    private const string InstitutionClaim = "eduassess:institution_id";
    private bool initialized;
    private Guid? selectedInstitutionId;

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

    public Guid? InstitutionId => selectedInstitutionId;

    /// <summary>NOVO: logo e cores da instituição em uso (nulo se ainda não carregou/selecionou).</summary>
    public InstitutionBrand? Brand { get; private set; }

    /// <summary>
    /// Marca efetiva do painel. Cada cor da unidade substitui somente a correspondente da instituição;
    /// o logotipo próprio da unidade também prevalece, e qualquer campo vazio herda da instituição.
    /// O preview de unidade do Admin usa a mesma regra.
    /// </summary>
    public InstitutionBrand? EffectiveBrand
    {
        get
        {
            if (Brand is null) return null;
            var unit = ThemeUnit;
            if (unit is null) return Brand;

            var hasUnitLogo = !string.IsNullOrWhiteSpace(unit.LogoPath) && images.HasUnitLogo(unit.Id);
            var logoVersion = hasUnitLogo ? images.UnitLogoVersion(unit.Id) : Brand.LogoVersion;
            var logoUrl = hasUnitLogo
                ? $"/ui/unidade-logo/{unit.Id}?v={logoVersion}"
                : Brand.ImageUrl;

            return Brand with
            {
                HasLogo = hasUnitLogo || Brand.HasLogo,
                LogoVersion = logoVersion,
                Primary = unit.Primary ?? Brand.Primary,
                Secondary = unit.Secondary ?? Brand.Secondary,
                CustomLogoUrl = logoUrl
            };
        }
    }

    /// <summary>Estilo inline do painel: tema estrutural + cores efetivas da unidade/instituição.</summary>
    public string? ShellStyle => string.Join(";", new[] { ThemeStyle, EffectiveBrand?.Style }.Where(x => !string.IsNullOrEmpty(x)));

    /// <summary>NOVO: informa ao interceptor de auditoria quem está operando neste circuito.</summary>
    private void SyncActor() =>
        auditActor.Set(User is null ? null : new AuditActor(User.Id, User.Name, UiFormat.RoleLabel(User.Role), selectedInstitutionId, null));
    public IReadOnlyList<InstitutionOption> AvailableInstitutions { get; private set; } = Array.Empty<InstitutionOption>();
    public bool RequiresInstitutionSelection => User is not null && AvailableInstitutions.Count > 1 && InstitutionId is null;

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

            if (Guid.TryParse(principal.FindFirst(InstitutionClaim)?.Value, out var institutionId))
                selectedInstitutionId = institutionId;
        }

        if (User is not null)
            await LoadUnitAsync(User.Id);

        SyncActor();
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
            var userRole = User?.Role;
            var institutionsQuery = db.UsuariosInstituicoes.AsNoTracking()
                .Where(ui => ui.UsuarioId == userId && ui.Ativa)
                .Select(ui => new
                {
                    ui.InstituicaoId,
                    Institution = ui.Instituicao!.Nome,
                    Tipo = ui.Instituicao!.Tipo,
                    Sigla = ui.Instituicao!.Sigla,
                    Criada = ui.Instituicao!.CriadaEm,
                    InstAtiva = ui.Instituicao!.Ativa,
                    Role = ui.Papel,
                    Unit = ui.Unidade == null ? null : new
                    {
                        ui.Unidade.Id, ui.Unidade.Nome, ui.Unidade.Slug, ui.Unidade.Sigla,
                        ui.Unidade.Tipo, ui.Unidade.Ativa, ui.Unidade.LogoPath,
                        ui.Unidade.CorPrimaria, ui.Unidade.CorSecundaria
                    }
                });

            // Só instituições ativas, da mais antiga para a mais nova (a mais antiga é a padrão).
            var links = (await institutionsQuery.ToListAsync()).Where(x => x.InstAtiva).OrderBy(x => x.Criada).ToList();
            var institutions = links.Select(x => new InstitutionOption(x.InstituicaoId, x.Institution, x.Tipo, x.Sigla)).ToList();

            if (userRole == UserRole.Admin)
            {
                institutions = await db.Instituicoes.AsNoTracking()
                    .Where(x => x.Ativa)
                    .OrderBy(x => x.CriadaEm)
                    .Select(x => new InstitutionOption(x.Id, x.Nome, x.Tipo, x.Sigla))
                    .ToListAsync();
            }

            AvailableInstitutions = institutions;
            if (selectedInstitutionId is not null && institutions.All(x => x.Id != selectedInstitutionId))
                selectedInstitutionId = null;
            // ALTERADO: com 2+ instituições (Faculdade e CLF) e nenhuma escolhida, usa a mais antiga em vez de bloquear o painel.
            // A troca é feita no seletor do topo (POST /ui/instituicao/selecionar), que grava a escolha no cookie.
            if (selectedInstitutionId is null && institutions.Count >= 1)
                selectedInstitutionId = institutions[0].Id;

            var link = selectedInstitutionId is Guid activeId
                ? links.FirstOrDefault(x => x.InstituicaoId == activeId)
                : null;

            if (User is not null && User.Role != UserRole.Admin)
                User = User with { Role = link is null ? UserRole.User : ToUserRole(link.Role) };

            Unit = link?.Unit is { Ativa: true } u
                ? new UnitInfo(u.Id, u.Nome, u.Slug, u.Sigla ?? "", u.Tipo, link.InstituicaoId, link.Institution,
                    u.LogoPath, u.CorPrimaria, u.CorSecundaria)
                : null;
            InstitutionName = institutions.FirstOrDefault(x => x.Id == selectedInstitutionId)?.Name ?? "Selecione uma instituição";

            Brand = selectedInstitutionId is Guid brandId ? await LoadBrandAsync(db, brandId) : null;

            CourseThemeKey = selectedInstitutionId is Guid institutionId
                ? await DetectCourseThemeAsync(db, userId, institutionId)
                : null;

            if (User?.Role == UserRole.Admin)
            {
                AllUnits = await db.Unidades.AsNoTracking()
                    .Where(x => x.Ativa && (selectedInstitutionId == null || x.InstituicaoId == selectedInstitutionId))
                    .OrderBy(x => x.Nome)
                    .Select(x => new UnitInfo(x.Id, x.Nome, x.Slug, x.Sigla ?? "", x.Tipo,
                        x.InstituicaoId, x.Instituicao!.Nome, x.LogoPath, x.CorPrimaria, x.CorSecundaria))
                    .ToListAsync();
            }
        }
        catch (Exception)
        {
            // Falhas no carregamento do contexto não devem ampliar o escopo nem revelar outro tenant.
            Unit = null;
            Brand = null;
            selectedInstitutionId = null;
            AvailableInstitutions = Array.Empty<InstitutionOption>();
        }
        SyncActor();
    }

    private async Task<InstitutionBrand?> LoadBrandAsync(AppDbContext db, Guid id)
    {
        var i = await db.Instituicoes.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Nome, x.Sigla, x.Tipo, x.LogoPath, x.CorPrimaria, x.CorSecundaria }).FirstOrDefaultAsync();
        if (i is null) return null;
        var hasLogo = !string.IsNullOrEmpty(i.LogoPath) && images.HasLogo(id);
        return new InstitutionBrand(id, i.Nome, i.Sigla, i.Tipo, hasLogo, hasLogo ? images.LogoVersion(id) : 0, i.CorPrimaria, i.CorSecundaria);
    }

    /// <summary>NOVO: recarrega logo e cores (usado depois de editar a identidade visual).</summary>
    public async Task RefreshBrandAsync()
    {
        if (selectedInstitutionId is not Guid id) return;
        await using var db = await dbFactory.CreateDbContextAsync();
        Brand = await LoadBrandAsync(db, id);
        Changed?.Invoke();
    }

    public async Task SelectInstitutionAsync(Guid institutionId)
    {
        await InitializeAsync();
        if (AvailableInstitutions.All(x => x.Id != institutionId))
            throw new UnauthorizedAccessException("Você não tem vínculo com essa instituição.");

        selectedInstitutionId = institutionId;
        await LoadUnitAsync(User!.Id);
        Changed?.Invoke();
    }

    private static UserRole ToUserRole(PapelInstituicao role) => role switch
    {
        PapelInstituicao.Aluno => UserRole.Aluno,
        PapelInstituicao.Professor => UserRole.Professor,
        PapelInstituicao.Gestor => UserRole.Gestor,
        _ => UserRole.User
    };

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

        var role = row.Role;
        if (row.Role != UserRole.Admin)
        {
            if (selectedInstitutionId is Guid institutionId)
            {
                var linkRole = await db.UsuariosInstituicoes.AsNoTracking()
                    .Where(x => x.UsuarioId == userId && x.InstituicaoId == institutionId && x.Ativa)
                    .Select(x => (PapelInstituicao?)x.Papel)
                    .FirstOrDefaultAsync();
                role = linkRole is null ? UserRole.User : ToUserRole(linkRole.Value);
            }
            else if (allowedRoles.Length > 0)
            {
                throw new UnauthorizedAccessException(RequiresInstitutionSelection
                    ? "Selecione uma instituição para continuar."
                    : "Você não tem vínculo ativo com uma instituição.");
            }
        }

        User = new UserSummary(userId, row.Name, row.Email, role);
        SyncActor();

        if (allowedRoles.Length > 0 && !allowedRoles.Contains(role))
            throw new UnauthorizedAccessException("Seu perfil não tem permissão para esta ação.");

        return User;
    }

    /// <summary>
    /// NOVO: o visual de Medicina vem do CURSO, não do prédio. Aluno: matrícula ativa em turma do curso de
    /// Medicina. Professor: turma ativa do curso de Medicina. "Medicina Veterinária" não conta.
    /// Regra pelo nome do curso (começa com "Medicina"); se o nome mudar, ajuste aqui.
    /// </summary>
    private async Task<string?> DetectCourseThemeAsync(AppDbContext db, Guid userId, Guid institutionId)
    {
        var role = User?.Role;
        bool inMedicine = false;

        if (role == UserRole.Aluno)
        {
            inMedicine = await db.Matriculas.AsNoTracking().AnyAsync(m =>
                m.InstituicaoId == institutionId && m.UsuarioId == userId && m.Ativa && m.Turma.Ativa && m.Turma.Curso.Ativo
                && EF.Functions.Like(m.Turma.Curso.Nome, "Medicina%")
                && !EF.Functions.Like(m.Turma.Curso.Nome, "%veterin%"));
        }
        else if (role == UserRole.Professor)
        {
            inMedicine = await db.Turmas.AsNoTracking().AnyAsync(t =>
                t.InstituicaoId == institutionId && t.ProfessorId == userId && t.Ativa && t.Curso.Ativo
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
