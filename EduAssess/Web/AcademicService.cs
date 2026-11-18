using EduAssess.Data;
using EduAssess.Models;
using EduAssess.Models.Local;
using EduAssess.Models.Global;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Web;

/// <summary>
/// Operações acadêmicas usadas pelo painel. Cada método revalida a sessão no banco e abre um
/// DbContext próprio (curto), evitando o uso concorrente de um contexto único dentro do circuito Blazor.
/// Obs.: o SQLite não agrega/ordena colunas decimal no servidor, então médias são calculadas em memória.
/// </summary>
public sealed class AcademicService(IDbContextFactory<AppDbContext> dbFactory, UiSession session)
{
    private static readonly UserRole[] Management = [UserRole.Admin, UserRole.Gestor];
    private static readonly UserRole[] Teaching = [UserRole.Admin, UserRole.Gestor, UserRole.Professor];

    // ───────────────────────── Painel de gestão ─────────────────────────

    public async Task<DashboardSummary> GetDashboardAsync()
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();

        var recent = await db.Matriculas.AsNoTracking().Where(x => x.InstituicaoId == institutionId)
            .OrderByDescending(x => x.DataMatricula)
            .Take(6)
            .Select(x => new EnrollmentRow(x.Id, x.Usuario.Name, x.Turma.Nome, x.Turma.Curso.Nome, x.DataMatricula, x.Ativa, x.UsuarioId, x.TurmaId))
            .ToListAsync();

        var courses = await db.Cursos.AsNoTracking().Where(x => x.InstituicaoId == institutionId && x.Ativo).OrderBy(x => x.Nome)
            .Select(x => new { x.Id, x.Nome }).ToListAsync();
        var grades = await db.Notas.AsNoTracking().Where(x => x.InstituicaoId == institutionId)
            .Select(x => new { x.Valor, CursoId = x.Matricula.Turma.CursoId }).ToListAsync();

        var performance = courses.Select(c =>
        {
            var values = grades.Where(g => g.CursoId == c.Id).Select(g => g.Valor).ToList();
            return new CoursePerformance(c.Nome, values.Count == 0 ? null : values.Average(), values.Count);
        }).ToList();

        return new DashboardSummary(
            courses.Count,
            await db.Turmas.CountAsync(x => x.InstituicaoId == institutionId && x.Ativa),
            await db.UsuariosInstituicoes.CountAsync(x => x.InstituicaoId == institutionId && x.Ativa && x.Papel == PapelInstituicao.Aluno),
            await db.UsuariosInstituicoes.CountAsync(x => x.InstituicaoId == institutionId && x.Ativa && x.Papel == PapelInstituicao.Professor),
            await db.Matriculas.CountAsync(x => x.InstituicaoId == institutionId && x.Ativa),
            await db.Users.CountAsync(x => x.Ativa && x.Role == UserRole.User),
            grades.Count,
            grades.Count == 0 ? null : grades.Average(g => g.Valor),
            await db.Contatos.CountAsync(x => !x.Lido),
            recent,
            performance);
    }

    // ───────────────────────── Cursos ─────────────────────────

    public async Task<List<CourseRow>> GetCoursesAsync(bool includeInactive = false)
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var classCounts = await db.Turmas.AsNoTracking().Where(t => t.InstituicaoId == institutionId && t.Ativa).Select(t => t.CursoId).ToListAsync();
        var query = db.Cursos.AsNoTracking().Where(x => x.InstituicaoId == institutionId);
        if (!includeInactive) query = query.Where(x => x.Ativo);
        var rows = await query.OrderBy(x => x.Nome)
            .Select(x => new { x.Id, x.Nome, x.Descricao, x.DuracaoEmAnos, x.Ativo }).ToListAsync();
        return rows.Select(x => new CourseRow(x.Id, x.Nome, x.Descricao, x.DuracaoEmAnos, classCounts.Count(c => c == x.Id), x.Ativo)).ToList();
    }

    public async Task SaveCourseAsync(Guid? id, string name, string? description, int duration)
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        var normalizedName = (name ?? "").Trim();
        if (normalizedName.Length < 3 || normalizedName.Length > 60)
            throw new ArgumentException("Informe um nome de curso entre 3 e 60 caracteres.");
        if (duration is < 1 or > 20)
            throw new ArgumentException("A duração deve ser de 1 a 20 anos.");
        var normalizedDescription = (description ?? "").Trim();
        if (normalizedDescription.Length > 500)
            throw new ArgumentException("A descrição deve ter até 500 caracteres.");

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Cursos.AnyAsync(x => x.InstituicaoId == institutionId && x.Ativo && x.Nome == normalizedName && x.Id != id))
            throw new ArgumentException("Já existe um curso ativo com esse nome.");

        if (id is null)
        {
            db.Cursos.Add(new CursoModel(normalizedName, normalizedDescription, duration, institutionId));
        }
        else
        {
            var course = await db.Cursos.FirstOrDefaultAsync(x => x.Id == id && x.InstituicaoId == institutionId)
                ?? throw new InvalidOperationException("O curso não foi encontrado.");
            course.Update(normalizedName, normalizedDescription, duration);
        }

        await db.SaveChangesAsync();
    }

    public async Task EnableCourseAsync(Guid id)
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var course = await db.Cursos.FirstOrDefaultAsync(x => x.Id == id && x.InstituicaoId == institutionId && !x.Ativo)
            ?? throw new InvalidOperationException("O curso inativo não foi encontrado.");
        if (await db.Cursos.AnyAsync(x => x.InstituicaoId == institutionId && x.Ativo && x.Nome == course.Nome && x.Id != id))
            throw new InvalidOperationException("Já existe um curso ativo com esse nome.");
        course.Enable();
        await db.SaveChangesAsync();
    }

    public async Task DisableCourseAsync(Guid id)
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var course = await db.Cursos.FirstOrDefaultAsync(x => x.Id == id && x.InstituicaoId == institutionId && x.Ativo)
            ?? throw new InvalidOperationException("O curso não foi encontrado.");
        course.Disable();
        await db.SaveChangesAsync();
    }

    // ───────────────────────── Disciplinas ─────────────────────────

    public async Task<List<DisciplineRow>> GetDisciplinesAsync(bool includeInactive = false)
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var teachers = (await db.UsuariosInstituicoes.AsNoTracking()
                .Where(x => x.InstituicaoId == institutionId && x.Ativa && x.Papel == PapelInstituicao.Professor)
                .Select(x => new { x.UsuarioId, Name = x.Usuario!.Name }).ToListAsync())
            .ToDictionary(u => u.UsuarioId, u => u.Name);
        var query = db.Disciplinas.AsNoTracking().Where(d => d.InstituicaoId == institutionId);
        if (!includeInactive) query = query.Where(d => d.Ativa);
        var rows = await query.OrderBy(d => d.Nome)
            .Select(d => new { d.Id, d.Nome, d.Descricao, d.ProfessorId, d.Ativa }).ToListAsync();
        return rows.Select(d => new DisciplineRow(
            d.Id, d.Nome, d.Descricao, d.ProfessorId,
            teachers.TryGetValue(d.ProfessorId, out var teacher) ? teacher : "Sem professor", d.Ativa)).ToList();
    }

    public async Task SaveDisciplineAsync(Guid? id, string name, string? description, Guid teacherId)
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        var normalizedName = (name ?? "").Trim();
        if (normalizedName.Length < 3 || normalizedName.Length > 100)
            throw new ArgumentException("Informe um nome de disciplina entre 3 e 100 caracteres.");
        var normalizedDescription = (description ?? "").Trim();
        if (normalizedDescription.Length > 500)
            throw new ArgumentException("A descrição deve ter até 500 caracteres.");

        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.UsuariosInstituicoes.AnyAsync(x => x.UsuarioId == teacherId && x.InstituicaoId == institutionId && x.Ativa && x.Papel == PapelInstituicao.Professor))
            throw new ArgumentException("Selecione um professor ativo.");
        if (await db.Disciplinas.AnyAsync(x => x.InstituicaoId == institutionId && x.Ativa && x.Nome == normalizedName && x.Id != id))
            throw new ArgumentException("Já existe uma disciplina ativa com esse nome.");

        if (id is null)
        {
            db.Disciplinas.Add(new DisciplinaModel(normalizedName, normalizedDescription, teacherId, institutionId));
        }
        else
        {
            var discipline = await db.Disciplinas.FirstOrDefaultAsync(x => x.Id == id && x.InstituicaoId == institutionId)
                ?? throw new InvalidOperationException("A disciplina não foi encontrada.");
            discipline.UpdateDisciplina(normalizedName, normalizedDescription, teacherId);
        }

        await db.SaveChangesAsync();
    }

    public async Task EnableDisciplineAsync(Guid id)
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var discipline = await db.Disciplinas.FirstOrDefaultAsync(x => x.Id == id && x.InstituicaoId == institutionId && !x.Ativa)
            ?? throw new InvalidOperationException("A disciplina inativa não foi encontrada.");
        if (!await db.UsuariosInstituicoes.AnyAsync(x => x.UsuarioId == discipline.ProfessorId && x.InstituicaoId == institutionId && x.Ativa && x.Papel == PapelInstituicao.Professor))
            throw new InvalidOperationException("A disciplina só pode ser reativada quando o professor responsável estiver ativo.");
        if (await db.Disciplinas.AnyAsync(x => x.InstituicaoId == institutionId && x.Ativa && x.Nome == discipline.Nome && x.Id != id))
            throw new InvalidOperationException("Já existe uma disciplina ativa com esse nome.");
        discipline.Enable();
        await db.SaveChangesAsync();
    }

    public async Task DisableDisciplineAsync(Guid id)
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var discipline = await db.Disciplinas.FirstOrDefaultAsync(x => x.Id == id && x.InstituicaoId == institutionId && x.Ativa)
            ?? throw new InvalidOperationException("A disciplina não foi encontrada.");
        discipline.Disable();
        await db.SaveChangesAsync();
    }

    // ───────────────────────── Pessoas ─────────────────────────

    public async Task<List<PersonRow>> GetStudentsAsync()
    {
        await session.RequireAsync(Teaching);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.UsuariosInstituicoes.AsNoTracking()
            .Where(x => x.InstituicaoId == institutionId && x.Ativa && x.Papel == PapelInstituicao.Aluno && x.Usuario!.Ativa)
            .OrderBy(x => x.Usuario!.Name)
            .Select(x => new PersonRow(x.UsuarioId, x.Usuario!.Name, x.Usuario.Email, x.Usuario.Telefone))
            .ToListAsync();
    }

    public async Task<List<StaffRow>> GetStaffAsync()
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.UsuariosInstituicoes.AsNoTracking()
            .Where(x => x.InstituicaoId == institutionId && x.Ativa && x.Usuario!.Ativa)
            .OrderBy(x => x.Usuario!.Name)
            .Select(x => new StaffRow(x.UsuarioId, x.Usuario!.Name, x.Usuario.Email, x.Papel == PapelInstituicao.Gestor ? UserRole.Gestor : UserRole.Professor))
            .ToListAsync();
    }

    public async Task<List<InstructorRow>> GetInstructorsAsync()
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.UsuariosInstituicoes.AsNoTracking()
            .Where(x => x.InstituicaoId == institutionId && x.Ativa && x.Papel == PapelInstituicao.Professor && x.Usuario!.Ativa)
            .OrderBy(x => x.Usuario!.Name)
            .Select(x => new InstructorRow(x.UsuarioId, x.Usuario!.Name, true))
            .ToListAsync();
    }

    // ───────────────────────── Turmas ─────────────────────────

    public async Task<List<ClassRow>> GetClassesAsync(bool includeInactive = false)
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var enrolled = await db.Matriculas.AsNoTracking().Where(m => m.InstituicaoId == institutionId && m.Ativa).Select(m => m.TurmaId).ToListAsync();
        var query = db.Turmas.AsNoTracking().Where(x => x.InstituicaoId == institutionId);
        if (!includeInactive) query = query.Where(x => x.Ativa);
        var rows = await query.OrderBy(x => x.Nome)
            .Select(x => new { x.Id, x.Nome, Curso = x.Curso.Nome, Professor = x.Professor.Name, x.Periodo, x.CursoId, x.ProfessorId, x.Ativa })
            .ToListAsync();
        return rows.Select(x => new ClassRow(x.Id, x.Nome, x.Curso, x.Professor, x.Periodo, enrolled.Count(t => t == x.Id), x.CursoId, x.ProfessorId, x.Ativa)).ToList();
    }

    public async Task SaveClassAsync(Guid? id, string name, Guid courseId, Guid instructorId)
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        var normalizedName = (name ?? "").Trim();
        if (normalizedName.Length < 2 || normalizedName.Length > 150)
            throw new ArgumentException("Informe o nome da turma (até 150 caracteres).");

        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.Cursos.AnyAsync(x => x.Id == courseId && x.InstituicaoId == institutionId && x.Ativo))
            throw new ArgumentException("Selecione um curso ativo.");
        if (!await db.UsuariosInstituicoes.AnyAsync(x => x.UsuarioId == instructorId && x.InstituicaoId == institutionId && x.Ativa && x.Papel == PapelInstituicao.Professor))
            throw new ArgumentException("Selecione um professor ativo.");
        if (await db.Turmas.AnyAsync(x => x.InstituicaoId == institutionId && x.Ativa && x.Nome == normalizedName && x.Id != id))
            throw new ArgumentException("Já existe uma turma ativa com esse nome.");

        if (id is null)
        {
            db.Turmas.Add(new TurmaModel(normalizedName, instructorId, courseId, institutionId));
        }
        else
        {
            var turma = await db.Turmas.FirstOrDefaultAsync(x => x.Id == id && x.InstituicaoId == institutionId)
                ?? throw new InvalidOperationException("A turma não foi encontrada.");
            turma.Update(normalizedName, instructorId, courseId, turma.Periodo);
        }
        await db.SaveChangesAsync();
    }

    public async Task SetClassActiveAsync(Guid id, bool active)
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var turma = await db.Turmas.FirstOrDefaultAsync(x => x.Id == id && x.InstituicaoId == institutionId)
            ?? throw new InvalidOperationException("A turma não foi encontrada.");
        if (active)
        {
            if (!turma.Curso.Ativo)
                throw new InvalidOperationException("Reative o curso antes de reativar esta turma.");
            if (!await db.UsuariosInstituicoes.AnyAsync(x => x.UsuarioId == turma.ProfessorId && x.InstituicaoId == institutionId && x.Ativa && x.Papel == PapelInstituicao.Professor))
                throw new InvalidOperationException("A turma só pode ser reativada quando o professor estiver ativo.");
            if (await db.Turmas.AnyAsync(x => x.InstituicaoId == institutionId && x.Ativa && x.Nome == turma.Nome && x.Id != id))
                throw new InvalidOperationException("Já existe uma turma ativa com esse nome.");
            turma.Enable();
        }
        else
        {
            turma.Disable();
        }
        await db.SaveChangesAsync();
    }

    // ───────────────────────── Matrículas ─────────────────────────

    public async Task<List<EnrollmentRow>> GetEnrollmentsAsync()
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Matriculas.AsNoTracking().Where(x => x.InstituicaoId == institutionId)
            .OrderByDescending(x => x.DataMatricula)
            .Select(x => new EnrollmentRow(x.Id, x.Usuario.Name, x.Turma.Nome, x.Turma.Curso.Nome, x.DataMatricula, x.Ativa, x.UsuarioId, x.TurmaId))
            .ToListAsync();
    }

    public async Task CreateEnrollmentAsync(Guid studentId, Guid classId)
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.UsuariosInstituicoes.AnyAsync(x => x.UsuarioId == studentId && x.InstituicaoId == institutionId && x.Ativa && x.Papel == PapelInstituicao.Aluno))
            throw new ArgumentException("Selecione um aluno ativo.");
        if (!await db.Turmas.AnyAsync(x => x.Id == classId && x.InstituicaoId == institutionId && x.Ativa))
            throw new ArgumentException("Selecione uma turma ativa.");
        if (await db.Matriculas.AnyAsync(x => x.InstituicaoId == institutionId && x.UsuarioId == studentId && x.TurmaId == classId && x.Ativa))
            throw new ArgumentException("Este aluno já está matriculado nesta turma.");

        db.Matriculas.Add(new MatriculaModel(studentId, classId, institutionId));
        await db.SaveChangesAsync();
    }

    public async Task UpdateEnrollmentAsync(Guid id, Guid studentId, Guid classId)
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var enrollment = await db.Matriculas.FirstOrDefaultAsync(x => x.Id == id && x.InstituicaoId == institutionId)
            ?? throw new InvalidOperationException("A matrícula não foi encontrada.");
        if (!await db.UsuariosInstituicoes.AnyAsync(x => x.UsuarioId == studentId && x.InstituicaoId == institutionId && x.Ativa && x.Papel == PapelInstituicao.Aluno))
            throw new ArgumentException("Selecione um aluno ativo.");
        if (!await db.Turmas.AnyAsync(x => x.Id == classId && x.InstituicaoId == institutionId && x.Ativa))
            throw new ArgumentException("Selecione uma turma ativa.");
        if (await db.Matriculas.AnyAsync(x => x.InstituicaoId == institutionId && x.Id != id && x.UsuarioId == studentId && x.TurmaId == classId && x.Ativa))
            throw new ArgumentException("Este aluno já está matriculado nesta turma.");
        enrollment.Update(studentId, classId);
        await db.SaveChangesAsync();
    }

    public async Task EndEnrollmentAsync(Guid id)
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var enrollment = await db.Matriculas.FirstOrDefaultAsync(x => x.Id == id && x.InstituicaoId == institutionId && x.Ativa)
            ?? throw new InvalidOperationException("A matrícula não foi encontrada.");
        enrollment.Disable();
        await db.SaveChangesAsync();
    }

    public async Task ReactivateEnrollmentAsync(Guid id)
    {
        await session.RequireAsync(Management);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var enrollment = await db.Matriculas.FirstOrDefaultAsync(x => x.Id == id && x.InstituicaoId == institutionId && !x.Ativa)
            ?? throw new InvalidOperationException("A matrícula encerrada não foi encontrada.");
        if (!await db.UsuariosInstituicoes.AnyAsync(x => x.UsuarioId == enrollment.UsuarioId && x.InstituicaoId == institutionId && x.Ativa && x.Papel == PapelInstituicao.Aluno))
            throw new InvalidOperationException("O aluno precisa estar ativo para reativar a matrícula.");
        if (!await db.Turmas.AnyAsync(x => x.Id == enrollment.TurmaId && x.InstituicaoId == institutionId && x.Ativa))
            throw new InvalidOperationException("A turma precisa estar ativa para reativar a matrícula.");
        if (await db.Matriculas.AnyAsync(x => x.InstituicaoId == institutionId && x.Id != id && x.UsuarioId == enrollment.UsuarioId && x.TurmaId == enrollment.TurmaId && x.Ativa))
            throw new InvalidOperationException("Já existe uma matrícula ativa deste aluno nesta turma.");
        enrollment.Enable();
        await db.SaveChangesAsync();
    }

    // ───────────────────────── Notas ─────────────────────────

    public async Task<List<GradeRow>> GetGradesAsync()
    {
        var actor = await session.RequireAsync(Teaching);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var actorId = actor.Id;
        var isTeacher = actor.Role == UserRole.Professor;

        var query = db.Notas.AsNoTracking().Where(n => n.InstituicaoId == institutionId).AsQueryable();
        if (isTeacher)
            query = query.Where(n => n.Matricula.Turma.ProfessorId == actorId);

        var rows = await query.OrderByDescending(n => n.DataLancamento)
            .Select(n => new
            {
                n.Id, n.DisciplinaId, n.UsuarioId, n.Descricao, n.Valor, n.DataLancamento,
                Aluno = n.Matricula.Usuario.Name, Turma = n.Matricula.Turma.Nome
            }).ToListAsync();

        var users = (await db.Users.AsNoTracking().Where(u => db.UsuariosInstituicoes.Any(ui => ui.UsuarioId == u.Id && ui.InstituicaoId == institutionId && ui.Ativa)).Select(u => new { u.Id, u.Name }).ToListAsync())
            .ToDictionary(u => u.Id, u => u.Name);
        var disciplines = (await db.Disciplinas.AsNoTracking().Where(d => d.InstituicaoId == institutionId).Select(d => new { d.Id, d.Nome }).ToListAsync())
            .ToDictionary(d => d.Id, d => d.Nome);

        return rows.Select(n => new GradeRow(
            n.Id, n.Aluno, n.Turma,
            disciplines.TryGetValue(n.DisciplinaId, out var discipline) ? discipline : "—",
            n.Descricao, n.Valor, n.DataLancamento,
            users.TryGetValue(n.UsuarioId, out var launcher) ? launcher : "—")).ToList();
    }

    /// <summary>Matrículas ativas em que o usuário pode lançar nota (professores: apenas suas turmas).</summary>
    public async Task<List<EnrollmentOption>> GetEnrollmentOptionsAsync()
    {
        var actor = await session.RequireAsync(Teaching);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var actorId = actor.Id;
        var query = db.Matriculas.AsNoTracking().Where(x => x.InstituicaoId == institutionId && x.Ativa);
        if (actor.Role == UserRole.Professor)
            query = query.Where(x => x.Turma.ProfessorId == actorId);

        var rows = await query.OrderBy(x => x.Usuario.Name)
            .Select(x => new { x.Id, Aluno = x.Usuario.Name, Turma = x.Turma.Nome }).ToListAsync();
        return rows.Select(x => new EnrollmentOption(x.Id, x.Aluno + " — " + x.Turma)).ToList();
    }

    /// <summary>Disciplinas em que o usuário pode lançar nota (professores: apenas as suas).</summary>
    public async Task<List<DisciplineOption>> GetDisciplineOptionsAsync()
    {
        var actor = await session.RequireAsync(Teaching);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var actorId = actor.Id;
        var query = db.Disciplinas.AsNoTracking().Where(x => x.InstituicaoId == institutionId && x.Ativa);
        if (actor.Role == UserRole.Professor)
            query = query.Where(x => x.ProfessorId == actorId);
        return await query.OrderBy(x => x.Nome).Select(x => new DisciplineOption(x.Id, x.Nome)).ToListAsync();
    }

    public async Task CreateGradeAsync(Guid enrollmentId, Guid disciplineId, string description, decimal value)
    {
        var actor = await session.RequireAsync(Teaching);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        var text = (description ?? "").Trim();
        if (text.Length < 2 || text.Length > 120)
            throw new ArgumentException("Informe a avaliação (2 a 120 caracteres).");
        if (value < 0 || value > 10)
            throw new ArgumentException("A nota deve estar entre 0 e 10.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var actorId = actor.Id;
        var isTeacher = actor.Role == UserRole.Professor;

        var enrollmentQuery = db.Matriculas.Where(x => x.Id == enrollmentId && x.InstituicaoId == institutionId && x.Ativa);
        if (isTeacher)
            enrollmentQuery = enrollmentQuery.Where(x => x.Turma.ProfessorId == actorId);
        if (!await enrollmentQuery.AnyAsync())
            throw new ArgumentException("Selecione uma matrícula ativa" + (isTeacher ? " de uma das suas turmas." : "."));

        var disciplineQuery = db.Disciplinas.Where(x => x.Id == disciplineId && x.InstituicaoId == institutionId && x.Ativa);
        if (isTeacher)
            disciplineQuery = disciplineQuery.Where(x => x.ProfessorId == actorId);
        if (!await disciplineQuery.AnyAsync())
            throw new ArgumentException("Selecione uma disciplina ativa" + (isTeacher ? " sob sua responsabilidade." : "."));

        db.Notas.Add(new NotaModel(enrollmentId, actorId, disciplineId, text, decimal.Round(value, 2), institutionId));
        await db.SaveChangesAsync();
    }

    // ───────────────────────── Painel do aluno ─────────────────────────

    public async Task<StudentOverview> GetStudentOverviewAsync()
    {
        var actor = await session.RequireAsync(UserRole.Aluno);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var actorId = actor.Id;

        var enrollments = await db.Matriculas.AsNoTracking()
            .Where(m => m.InstituicaoId == institutionId && m.UsuarioId == actorId)
            .OrderByDescending(m => m.DataMatricula)
            .Select(m => new MyEnrollmentRow(m.Turma.Nome, m.Turma.Curso.Nome, m.Turma.Professor.Name, m.DataMatricula, m.DataExpiracao, m.Ativa))
            .ToListAsync();

        var raw = await db.Notas.AsNoTracking()
            .Where(n => n.InstituicaoId == institutionId && n.Matricula.UsuarioId == actorId)
            .OrderByDescending(n => n.DataLancamento)
            .Select(n => new { n.DisciplinaId, n.Descricao, n.Valor, n.DataLancamento, Turma = n.Matricula.Turma.Nome })
            .ToListAsync();
        var disciplines = (await db.Disciplinas.AsNoTracking().Where(d => d.InstituicaoId == institutionId).Select(d => new { d.Id, d.Nome }).ToListAsync())
            .ToDictionary(d => d.Id, d => d.Nome);

        var grades = raw.Select(n => new MyGradeRow(
            disciplines.TryGetValue(n.DisciplinaId, out var name) ? name : "Disciplina",
            n.Turma, n.Descricao, n.Valor, n.DataLancamento)).ToList();

        var averages = grades.GroupBy(g => g.DisciplineName)
            .Select(g => new DisciplineAverage(g.Key, g.Average(x => x.Value), g.Count()))
            .OrderBy(a => a.DisciplineName).ToList();

        return new StudentOverview(enrollments, grades, averages, grades.Count == 0 ? null : grades.Average(g => g.Value));
    }

    // ───────────────────────── Painel do professor ─────────────────────────

    public async Task<TeacherOverview> GetTeacherOverviewAsync()
    {
        var actor = await session.RequireAsync(UserRole.Professor);
        var institutionId = session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var actorId = actor.Id;

        var classes = await db.Turmas.AsNoTracking().Where(t => t.InstituicaoId == institutionId && t.Ativa && t.ProfessorId == actorId)
            .OrderBy(t => t.Nome)
            .Select(t => new { t.Id, t.Nome, Curso = t.Curso.Nome, t.Periodo }).ToListAsync();
        var enrolled = await db.Matriculas.AsNoTracking()
            .Where(m => m.InstituicaoId == institutionId && m.Ativa && m.Turma.ProfessorId == actorId)
            .Select(m => new { m.TurmaId, m.UsuarioId }).ToListAsync();
        var disciplineCount = await db.Disciplinas.CountAsync(d => d.InstituicaoId == institutionId && d.Ativa && d.ProfessorId == actorId);
        var gradeCount = await db.Notas.CountAsync(n => n.InstituicaoId == institutionId && n.UsuarioId == actorId);

        var recent = (await GetGradesAsync()).Take(6).ToList();

        return new TeacherOverview(
            classes.Select(c => new TeacherClassRow(c.Nome, c.Curso, c.Periodo, enrolled.Count(e => e.TurmaId == c.Id))).ToList(),
            enrolled.Select(e => e.UsuarioId).Distinct().Count(),
            disciplineCount, gradeCount, recent);
    }

    // ───────────────────────── Contatos ─────────────────────────

    public async Task<List<ContactRow>> GetContactsAsync()
    {
        await session.RequireAsync(Management);
        if (session.InstitutionId is null)
            throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Contatos.AsNoTracking().OrderByDescending(c => c.CriadoEm)
            .Select(c => new ContactRow(c.Id, c.Nome, c.Email, c.Telefone, c.Instituicao, c.Papel, c.Mensagem, c.CriadoEm, c.Lido))
            .ToListAsync();
    }

    public async Task MarkContactReadAsync(Guid id)
    {
        await session.RequireAsync(Management);
        if (session.InstitutionId is null)
            throw new UnauthorizedAccessException("Selecione uma instituição.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var contact = await db.Contatos.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("A solicitação não foi encontrada.");
        contact.MarcarComoLido();
        await db.SaveChangesAsync();
    }
}
