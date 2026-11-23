using EduAssess.Application.DTOs.Instituicao;
using EduAssess.Application.DTOs.Unidade;
using EduAssess.Application.DTOs.UsuarioInstituicao;
using EduAssess.Application.Services;
using EduAssess.Data;
using EduAssess.Models;
using EduAssess.Models.Global;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Web;

/// <summary>
/// NOVO: serviços da tela "Instituição e unidades" (somente Admin e Gestor).
/// Reaproveita as MESMAS regras da API (InstituicaoService, UnidadeService e UsuarioInstituicaoService),
/// criando cada serviço com um DbContext curto da fábrica; assim a validação (CNPJ repetido, sigla repetida,
/// unidade com gente) é uma só, venha a ação da tela ou da API.
/// </summary>
public sealed class InstitutionWebService(IDbContextFactory<AppDbContext> dbFactory, UiSession session, ImageStorageService images)
{
    private static readonly UserRole[] Management = [UserRole.Admin, UserRole.Gestor];

    // ───────────────────────── Visão geral ─────────────────────────

    /// <summary>
    /// ALTERADO: a instituição EM USO (a escolhida no seletor do topo; antes era "a primeira ativa"), as
    /// unidades e quantas pessoas há em cada uma. Traz também as irmãs da rede (mesmo mantenedor).
    /// </summary>
    public async Task<InstitutionOverview> GetOverviewAsync()
    {
        await session.RequireAsync(Management);
        await using var db = await dbFactory.CreateDbContextAsync();

        var currentId = session.InstitutionId;
        var institution = await db.Instituicoes.AsNoTracking().Include(i => i.Mantenedor)
            .Where(i => i.Ativa && i.Id == currentId).FirstOrDefaultAsync();
        if (institution is null)
            return new InstitutionOverview(null, new(), 0);

        var units = await db.Unidades.AsNoTracking()
            .Where(u => u.InstituicaoId == institution.Id).OrderBy(u => u.Nome).ToListAsync();

        var links = await db.UsuariosInstituicoes.AsNoTracking()
            .Where(ui => ui.InstituicaoId == institution.Id && ui.Ativa)
            .Select(ui => new { ui.UnidadeId, ui.Papel }).ToListAsync();

        var cards = units.Select(u => new UnitCard(
            u.Id, u.Nome, u.Slug, u.Sigla ?? "", u.Tipo, u.Ativa,
            links.Count(l => l.UnidadeId == u.Id && l.Papel == PapelInstituicao.Aluno),
            links.Count(l => l.UnidadeId == u.Id && l.Papel == PapelInstituicao.Professor),
            links.Count(l => l.UnidadeId == u.Id && l.Papel == PapelInstituicao.Gestor))).ToList();

        // Pessoas vinculadas a ESTA instituição sem unidade definida, e pessoas ativas ainda sem vínculo com ela.
        var withoutUnit = links.Count(l => l.UnidadeId == null);
        var linkedIds = await db.UsuariosInstituicoes.AsNoTracking()
            .Where(ui => ui.InstituicaoId == institution.Id && ui.Ativa).Select(ui => ui.UsuarioId).ToListAsync();
        var unlinked = await db.Users.AsNoTracking().CountAsync(u => u.Ativa && u.Role != UserRole.User && !linkedIds.Contains(u.Id));

        // NOVO: o visual de Medicina vale para quem é do CURSO de Medicina (qualquer prédio): contamos aqui.
        var medicineStudents = await db.Matriculas.AsNoTracking()
            .Where(m => m.InstituicaoId == institution.Id && m.Ativa && m.Turma.Ativa && m.Turma.Curso.Ativo
                        && EF.Functions.Like(m.Turma.Curso.Nome, "Medicina%") && !EF.Functions.Like(m.Turma.Curso.Nome, "%veterin%"))
            .Select(m => m.UsuarioId).Distinct().CountAsync();
        var medicineProfessors = await db.Turmas.AsNoTracking()
            .Where(t => t.InstituicaoId == institution.Id && t.Ativa && t.Curso.Ativo
                        && EF.Functions.Like(t.Curso.Nome, "Medicina%") && !EF.Functions.Like(t.Curso.Nome, "%veterin%"))
            .Select(t => t.ProfessorId).Distinct().CountAsync();

        // Rede: instituições do mesmo mantenedor (Faculdade + Colégio...), para alternar e ver o vínculo.
        List<NetworkInstitution>? network = null;
        if (institution.MantenedorId is Guid mid)
            network = await db.Instituicoes.AsNoTracking().Where(i => i.MantenedorId == mid).OrderBy(i => i.CriadaEm)
                .Select(i => new NetworkInstitution(i.Id, i.Nome, i.Sigla, i.Tipo, i.Ativa, i.Id == institution.Id)).ToListAsync();

        // O Admin escolhe entre todos os mantenedores; o Gestor só enxerga o da própria instituição.
        var isAdmin = session.User?.Role == UserRole.Admin;
        var mantenedores = await db.Mantenedores.AsNoTracking()
            .Where(m => isAdmin || m.Id == institution.MantenedorId).OrderBy(m => m.Nome)
            .Select(m => new MantenedorOption(m.Id, m.Nome)).ToListAsync();

        return new InstitutionOverview(
            new InstitutionInfo(institution.Id, institution.Nome, institution.Cnpj, institution.Email,
                institution.Telefone, institution.Endereco, institution.Ativa,
                institution.Tipo, institution.Sigla, institution.MantenedorId, institution.Mantenedor?.Nome,
                institution.CorPrimaria, institution.CorSecundaria,
                !string.IsNullOrEmpty(institution.LogoPath) && images.HasLogo(institution.Id), images.LogoVersion(institution.Id)),
            cards, withoutUnit, medicineStudents, medicineProfessors, network, mantenedores, unlinked);
    }

    // ───────────────────────── Instituição ─────────────────────────

    public async Task SaveInstitutionAsync(Guid id, InstitutionForm form)
    {
        var me = await session.RequireAsync(Management);
        RequireCurrent(id);
        Validate(form, checkClassification: me.Role == UserRole.Admin);   // Gestor não altera categoria/mantenedor
        await using var db = await dbFactory.CreateDbContextAsync();
        var service = new InstituicaoService(db);
        // Só o Admin muda categoria e mantenedor; para o Gestor esses campos ficam como estão (nulo = manter).
        var admin = me.Role == UserRole.Admin;
        try
        {
            await service.AtualizarInstituicaoAsync(id, new InstituicaoRequestPut(
                form.Name.Trim(), form.Cnpj.Trim(), form.Address.Trim(), form.Phone.Trim(), form.Email.Trim(),
                admin ? form.Tipo.ToString() : null, form.Sigla.Trim(),
                admin ? form.MantenedorId : null, admin && form.MantenedorId is null ? form.NewMantenedorName : null));
        }
        catch (KeyNotFoundException ex) { throw new InvalidOperationException(ex.Message); }
    }

    /// <summary>NOVO: cadastra uma instituição nova (Faculdade ou Colégio/CLF). Só o Admin. Devolve o ID criado.</summary>
    public async Task<Guid> CreateInstitutionAsync(InstitutionForm form)
    {
        await session.RequireAsync(UserRole.Admin);
        Validate(form);
        await using var db = await dbFactory.CreateDbContextAsync();
        var service = new InstituicaoService(db);
        try
        {
            var created = await service.CriarInstituicaoAsync(new InstituicaoRequest(
                form.Name.Trim(), form.Cnpj.Trim(), form.Address.Trim(), form.Phone.Trim(), form.Email.Trim(),
                form.Tipo.ToString(), form.Sigla.Trim(), form.MantenedorId, form.MantenedorId is null ? form.NewMantenedorName : null));
            return created.Id;
        }
        catch (KeyNotFoundException ex) { throw new InvalidOperationException(ex.Message); }
    }

    // ───────────────────────── NOVO: identidade visual ─────────────────────────

    public async Task SaveBrandingAsync(Guid id, string primary, string secondary)
    {
        await session.RequireAsync(Management);
        RequireCurrent(id);
        await using var db = await dbFactory.CreateDbContextAsync();
        try { await new InstituicaoService(db).AtualizarIdentidadeAsync(id, new InstituicaoIdentidadeRequest(primary, secondary)); }
        catch (KeyNotFoundException ex) { throw new InvalidOperationException(ex.Message); }
        await session.RefreshBrandAsync();
    }

    public async Task<AvatarResult> UploadLogoAsync(Guid id, Stream file)
    {
        await session.RequireAsync(Management);
        RequireCurrent(id);
        var result = await images.SaveLogoAsync(id, file);
        await using var db = await dbFactory.CreateDbContextAsync();
        try { await new InstituicaoService(db).DefinirLogoAsync(id, true); }
        catch (KeyNotFoundException ex) { images.DeleteLogo(id); throw new InvalidOperationException(ex.Message); }
        await session.RefreshBrandAsync();
        return result;
    }

    public async Task RemoveLogoAsync(Guid id)
    {
        await session.RequireAsync(Management);
        RequireCurrent(id);
        await using var db = await dbFactory.CreateDbContextAsync();
        try { await new InstituicaoService(db).DefinirLogoAsync(id, false); }
        catch (KeyNotFoundException ex) { throw new InvalidOperationException(ex.Message); }
        images.DeleteLogo(id);
        await session.RefreshBrandAsync();
    }

    /// <summary>Só se edita a instituição que está em uso (nada de id arbitrário vindo do navegador).</summary>
    private void RequireCurrent(Guid id)
    {
        if (session.InstitutionId != id)
            throw new UnauthorizedAccessException("Você só pode alterar a instituição que está em uso.");
    }

    // ───────────────────────── Unidades ─────────────────────────

    public async Task SaveUnitAsync(Guid institutionId, Guid? unitId, UnitForm form)
    {
        await session.RequireAsync(Management);
        RequireCurrent(institutionId);
        var name = (form.Name ?? "").Trim();
        var sigla = (form.Sigla ?? "").Trim().ToUpperInvariant();
        if (name.Length is < 3 or > 60)
            throw new ArgumentException("O nome da unidade deve ter entre 3 e 60 caracteres.");
        if (sigla.Length is < 2 or > 10)
            throw new ArgumentException("A sigla deve ter entre 2 e 10 caracteres.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var service = new UnidadeService(db);
        try
        {
            if (unitId is null)
                await service.CriarUnidadeAsync(new UnidadeRequest(institutionId, name, form.Type, sigla));
            else
                await service.AtualizarUnidadeAsync(unitId.Value, new UnidadeRequestPut(name, sigla, form.Type));
        }
        catch (KeyNotFoundException ex) { throw new InvalidOperationException(ex.Message); }
    }

    public async Task SetUnitActiveAsync(Guid unitId, bool active)
    {
        await session.RequireAsync(Management);
        await using var db = await dbFactory.CreateDbContextAsync();
        var service = new UnidadeService(db);
        try
        {
            if (active) await service.AtivarUnidadeAsync(unitId);
            else await service.DeletarUnidadeAsync(unitId);   // bloqueia se ainda houver gente lotada
        }
        catch (KeyNotFoundException ex) { throw new InvalidOperationException(ex.Message); }
    }

    // ───────────────────────── Pessoas ─────────────────────────

    /// <summary>
    /// Pessoas ativas (aluno, professor, gestor) com a unidade em que estão. unitFilter: nulo = todas;
    /// Guid.Empty = só quem não tem unidade; outro valor = só aquela unidade.
    /// </summary>
    public async Task<List<UnitPersonRow>> GetPeopleAsync(Guid? unitFilter, string? search, bool onlyUnlinked = false)
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();

        var users = await db.Users.AsNoTracking()
            .Where(u => u.Ativa && u.Role != UserRole.User)
            .Select(u => new { u.Id, u.Name, u.Email, u.Role })
            .ToListAsync();

        // ALTERADO: só os vínculos DESTA instituição (antes eram de qualquer uma).
        var links = await db.UsuariosInstituicoes.AsNoTracking()
            .Where(ui => ui.Ativa && ui.InstituicaoId == institutionId)
            .Select(ui => new { ui.UsuarioId, ui.UnidadeId })
            .ToListAsync();
        var units = (await db.Unidades.AsNoTracking().Where(u => u.InstituicaoId == institutionId).Select(u => new { u.Id, u.Nome, u.Slug }).ToListAsync())
            .ToDictionary(u => u.Id);

        var rows = users.Select(u =>
        {
            var link = links.FirstOrDefault(l => l.UsuarioId == u.Id);
            var unit = link?.UnidadeId is Guid uid && units.TryGetValue(uid, out var x) ? x : null;
            return new UnitPersonRow(u.Id, u.Name, u.Email, u.Role, unit?.Id, unit?.Nome, unit?.Slug, link is not null);
        });

        // "Não vinculadas" = pessoas ativas que ainda não fazem parte desta instituição; as demais listas só têm vinculadas.
        rows = onlyUnlinked ? rows.Where(r => !r.Linked) : rows.Where(r => r.Linked);

        if (unitFilter == Guid.Empty) rows = rows.Where(r => r.UnitId is null);
        else if (unitFilter is Guid f) rows = rows.Where(r => r.UnitId == f);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            rows = rows.Where(r => r.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                                   || r.Email.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return rows.OrderBy(r => r.Name).Take(300).ToList();
    }

    /// <summary>
    /// Define (ou muda) a unidade de uma pessoa. Se ela ainda não tem vínculo com a instituição, cria um
    /// já com o papel correspondente ao perfil; unitId nulo tira a pessoa de qualquer unidade.
    /// </summary>
    public async Task AssignUnitAsync(Guid userId, Guid? unitId)
    {
        await session.RequireAsync(Management);
        await using var db = await dbFactory.CreateDbContextAsync();

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && u.Ativa)
            ?? throw new InvalidOperationException("Pessoa não encontrada.");

        var institutionId = session.InstitutionId ?? Guid.Empty;
        if (institutionId == Guid.Empty)
            throw new InvalidOperationException("Cadastre a instituição antes de definir unidades.");

        var service = new UsuarioInstituicaoService(db);
        var papel = PapelFor(user.Role).ToString();

        var existing = await db.UsuariosInstituicoes
            .FirstOrDefaultAsync(ui => ui.UsuarioId == userId && ui.InstituicaoId == institutionId);

        try
        {
            if (existing is { Ativa: true })
                await service.AtualizarUsuarioInstituicaoAsync(new UsuarioInstituicaoRequestPut(userId, institutionId, papel, unitId));
            else
                await service.VincularUsuarioInstituicaoAsync(new UsuarioInstituicaoRequest(userId, institutionId, papel, unitId));
        }
        catch (KeyNotFoundException ex) { throw new InvalidOperationException(ex.Message); }
    }

    /// <summary>NOVO: vincula (ou desvincula) uma pessoa à instituição em uso. Vincular já usa o papel do perfil dela.</summary>
    public async Task SetLinkAsync(Guid userId, bool linked)
    {
        var me = await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var service = new UsuarioInstituicaoService(db);
        try
        {
            if (linked)
            {
                var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && u.Ativa && u.Role != UserRole.User)
                    ?? throw new InvalidOperationException("Pessoa não encontrada.");
                await service.VincularUsuarioInstituicaoAsync(new UsuarioInstituicaoRequest(userId, institutionId, PapelFor(user.Role).ToString(), null));
            }
            else
            {
                if (userId == me.Id) throw new InvalidOperationException("Você não pode desvincular a si mesmo.");
                await service.DesvincularUsuarioInstituicaoAsync(userId, institutionId);
            }
        }
        catch (KeyNotFoundException ex) { throw new InvalidOperationException(ex.Message); }
    }

    /// <summary>Papel institucional que corresponde ao perfil do sistema.</summary>
    public static PapelInstituicao PapelFor(UserRole role) => role switch
    {
        UserRole.Aluno => PapelInstituicao.Aluno,
        UserRole.Professor => PapelInstituicao.Professor,
        _ => PapelInstituicao.Gestor
    };

    private static void Validate(InstitutionForm form, bool checkClassification = true)
    {
        if ((form.Name ?? "").Trim().Length is < 3 or > 120)
            throw new ArgumentException("O nome da instituição deve ter entre 3 e 120 caracteres.");
        if ((form.Cnpj ?? "").Trim().Length is < 14 or > 18)
            throw new ArgumentException("Informe o CNPJ (14 dígitos, com ou sem pontuação).");
        if ((form.Email ?? "").Length > 120 || (form.Phone ?? "").Length > 30 || (form.Address ?? "").Length > 200)
            throw new ArgumentException("Algum campo passou do tamanho permitido.");
        if ((form.Sigla ?? "").Trim().Length > 10)
            throw new ArgumentException("A sigla deve ter até 10 caracteres.");
        if (checkClassification && form.Tipo == TipoInstituicao.Colegio && form.MantenedorId is null && (form.NewMantenedorName ?? "").Trim().Length == 0)
            throw new ArgumentException("O colégio precisa estar vinculado a um mantenedor: escolha um ou informe o nome.");
    }
}
