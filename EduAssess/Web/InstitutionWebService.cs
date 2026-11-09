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
public sealed class InstitutionWebService(IDbContextFactory<AppDbContext> dbFactory, UiSession session)
{
    private static readonly UserRole[] Management = [UserRole.Admin, UserRole.Gestor];

    // ───────────────────────── Visão geral ─────────────────────────

    /// <summary>A instituição (a primeira ativa), as unidades e quantas pessoas há em cada uma.</summary>
    public async Task<InstitutionOverview> GetOverviewAsync()
    {
        await session.RequireAsync(Management);
        await using var db = await dbFactory.CreateDbContextAsync();

        var institution = await db.Instituicoes.AsNoTracking()
            .Where(i => i.Ativa).OrderBy(i => i.Nome).FirstOrDefaultAsync();
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

        // Pessoas ativas do sistema (alunos, professores, gestores) que ainda não têm unidade definida.
        var withUnit = links.Where(l => l.UnidadeId != null).Count();
        var staffAndStudents = await db.Users.AsNoTracking().CountAsync(u => u.Ativa && u.Role != UserRole.User && u.Role != UserRole.Admin);
        var withoutUnit = Math.Max(0, staffAndStudents - withUnit);

        // NOVO: o visual de Medicina vale para quem é do CURSO de Medicina (qualquer prédio): contamos aqui.
        var medicineStudents = await db.Matriculas.AsNoTracking()
            .Where(m => m.Ativa && m.Turma.Ativa && m.Turma.Curso.Ativo
                        && EF.Functions.Like(m.Turma.Curso.Nome, "Medicina%") && !EF.Functions.Like(m.Turma.Curso.Nome, "%veterin%"))
            .Select(m => m.UsuarioId).Distinct().CountAsync();
        var medicineProfessors = await db.Turmas.AsNoTracking()
            .Where(t => t.Ativa && t.Curso.Ativo
                        && EF.Functions.Like(t.Curso.Nome, "Medicina%") && !EF.Functions.Like(t.Curso.Nome, "%veterin%"))
            .Select(t => t.ProfessorId).Distinct().CountAsync();

        return new InstitutionOverview(
            new InstitutionInfo(institution.Id, institution.Nome, institution.Cnpj, institution.Email,
                institution.Telefone, institution.Endereco, institution.Ativa),
            cards, withoutUnit, medicineStudents, medicineProfessors);
    }

    // ───────────────────────── Instituição ─────────────────────────

    public async Task SaveInstitutionAsync(Guid id, InstitutionForm form)
    {
        await session.RequireAsync(Management);
        Validate(form);
        await using var db = await dbFactory.CreateDbContextAsync();
        var service = new InstituicaoService(db);
        try
        {
            await service.AtualizarInstituicaoAsync(id, new InstituicaoRequestPut(
                form.Name.Trim(), form.Cnpj.Trim(), form.Address.Trim(), form.Phone.Trim(), form.Email.Trim()));
        }
        catch (KeyNotFoundException ex) { throw new InvalidOperationException(ex.Message); }
    }

    // ───────────────────────── Unidades ─────────────────────────

    public async Task SaveUnitAsync(Guid institutionId, Guid? unitId, UnitForm form)
    {
        await session.RequireAsync(Management);
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
    public async Task<List<UnitPersonRow>> GetPeopleAsync(Guid? unitFilter, string? search)
    {
        await session.RequireAsync(Management);
        await using var db = await dbFactory.CreateDbContextAsync();

        var users = await db.Users.AsNoTracking()
            .Where(u => u.Ativa && u.Role != UserRole.User)
            .Select(u => new { u.Id, u.Name, u.Email, u.Role })
            .ToListAsync();

        var links = await db.UsuariosInstituicoes.AsNoTracking()
            .Where(ui => ui.Ativa)
            .Select(ui => new { ui.UsuarioId, ui.UnidadeId })
            .ToListAsync();
        var units = (await db.Unidades.AsNoTracking().Select(u => new { u.Id, u.Nome, u.Slug }).ToListAsync())
            .ToDictionary(u => u.Id);

        var rows = users.Select(u =>
        {
            var link = links.FirstOrDefault(l => l.UsuarioId == u.Id);
            var unit = link?.UnidadeId is Guid uid && units.TryGetValue(uid, out var x) ? x : null;
            return new UnitPersonRow(u.Id, u.Name, u.Email, u.Role, unit?.Id, unit?.Nome, unit?.Slug, link is not null);
        });

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

        var institutionId = await db.Instituicoes.Where(i => i.Ativa).OrderBy(i => i.Nome).Select(i => i.Id).FirstOrDefaultAsync();
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

    /// <summary>Papel institucional que corresponde ao perfil do sistema.</summary>
    public static PapelInstituicao PapelFor(UserRole role) => role switch
    {
        UserRole.Aluno => PapelInstituicao.Aluno,
        UserRole.Professor => PapelInstituicao.Professor,
        _ => PapelInstituicao.Gestor
    };

    private static void Validate(InstitutionForm form)
    {
        if ((form.Name ?? "").Trim().Length is < 3 or > 120)
            throw new ArgumentException("O nome da instituição deve ter entre 3 e 120 caracteres.");
        if ((form.Cnpj ?? "").Trim().Length is < 14 or > 18)
            throw new ArgumentException("Informe o CNPJ (14 dígitos, com ou sem pontuação).");
        if ((form.Email ?? "").Length > 120 || (form.Phone ?? "").Length > 30 || (form.Address ?? "").Length > 200)
            throw new ArgumentException("Algum campo passou do tamanho permitido.");
    }
}
