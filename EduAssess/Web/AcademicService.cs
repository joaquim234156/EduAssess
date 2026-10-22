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
        await using var db = await dbFactory.CreateDbContextAsync();

        var recent = await db.Matriculas.AsNoTracking()
            .OrderByDescending(x => x.DataMatricula)
            .Take(6)
            .Select(x => new EnrollmentRow(x.Id, x.Usuario.Name, x.Turma.Nome, x.Turma.Curso.Nome, x.DataMatricula, x.Ativa))
            .ToListAsync();

        var courses = await db.Cursos.AsNoTracking().Where(x => x.Ativo).OrderBy(x => x.Nome)
            .Select(x => new { x.Id, x.Nome }).ToListAsync();
        var grades = await db.Notas.AsNoTracking()
            .Select(x => new { x.Valor, CursoId = x.Matricula.Turma.CursoId }).ToListAsync();

        var performance = courses.Select(c =>
        {
            var values = grades.Where(g => g.CursoId == c.Id).Select(g => g.Valor).ToList();
            return new CoursePerformance(c.Nome, values.Count == 0 ? null : values.Average(), values.Count);
        }).ToList();

        return new DashboardSummary(
            courses.Count,
            await db.Turmas.CountAsync(x => x.Ativo),
            await db.Users.CountAsync(x => x.Ativa && x.Role == UserRole.Aluno),
            await db.Users.CountAsync(x => x.Ativa && x.Role == UserRole.Professor),
            await db.Matriculas.CountAsync(x => x.Ativa),
            await db.Users.CountAsync(x => x.Ativa && x.Role == UserRole.User),
            grades.Count,
            grades.Count == 0 ? null : grades.Average(g => g.Valor),
            await db.Contatos.CountAsync(x => !x.Lido),
            recent,
            performance);
    }

    // ───────────────────────── Cursos ─────────────────────────

    public async Task<List<CourseRow>> GetCoursesAsync()
    {
        await session.RequireAsync(Management);
        await using var db = await dbFactory.CreateDbContextAsync();
        var classCounts = await db.Turmas.AsNoTracking().Where(t => t.Ativo).Select(t => t.CursoId).ToListAsync();
        var rows = await db.Cursos.AsNoTracking().Where(x => x.Ativo).OrderBy(x => x.Nome)
            .Select(x => new { x.Id, x.Nome, x.Descricao, x.DuracaoEmAnos }).ToListAsync();
        return rows.Select(x => new CourseRow(x.Id, x.Nome, x.Descricao, x.DuracaoEmAnos, classCounts.Count(c => c == x.Id))).ToList();
    }

    public async Task SaveCourseAsync(Guid? id, string name, string? description, int duration)
    {
        await session.RequireAsync(Management);
        var normalizedName = (name ?? "").Trim();
        if (normalizedName.Length < 3 || normalizedName.Length > 60)
            throw new ArgumentException("Informe um nome de curso entre 3 e 60 caracteres.");
        if (duration is < 1 or > 20)
            throw new ArgumentException("A duração deve ser de 1 a 20 anos.");
        var normalizedDescription = (description ?? "").Trim();
        if (normalizedDescription.Length > 500)
            throw new ArgumentException("A descrição deve ter até 500 caracteres.");

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Cursos.AnyAsync(x => x.Ativo && x.Nome == normalizedName && x.Id != id))
            throw new ArgumentException("Já existe um curso ativo com esse nome.");

        if (id is null)
        {
            db.Cursos.Add(new CursoModel(normalizedName, normalizedDescription, duration));
        }
        else
        {
            var course = await db.Cursos.FirstOrDefaultAsync(x => x.Id == id && x.Ativo)
                ?? throw new InvalidOperationException("O curso não foi encontrado.");
            course.Update(normalizedName, normalizedDescription, duration);
        }

        await db.SaveChangesAsync();
    }

    public async Task DisableCourseAsync(Guid id)
    {
        await session.RequireAsync(Management);
        await using var db = await dbFactory.CreateDbContextAsync();
        var course = await db.Cursos.FirstOrDefaultAsync(x => x.Id == id && x.Ativo)
            ?? throw new InvalidOperationException("O curso não foi encontrado.");
        course.Disable();
        await db.SaveChangesAsync();
    }

    // ───────────────────────── Disciplinas ─────────────────────────

    public async Task<List<DisciplineRow>> GetDisciplinesAsync()
    {
        await session.RequireAsync(Management);
        await using var db = await dbFactory.CreateDbContextAsync();
        var teachers = (await db.Users.AsNoTracking().Select(u => new { u.Id, u.Name }).ToListAsync())
            .ToDictionary(u => u.Id, u => u.Name);
        var rows = await db.Disciplinas.AsNoTracking().Where(d => d.Ativa).OrderBy(d => d.Nome)
            .Select(d => new { d.Id, d.Nome, d.Descricao, d.ProfessorId }).ToListAsync();
        return rows.Select(d => new DisciplineRow(
            d.Id, d.Nome, d.Descricao, d.ProfessorId,
            teachers.TryGetValue(d.ProfessorId, out var teacher) ? teacher : "Sem professor")).ToList();
    }

    public async Task SaveDisciplineAsync(Guid? id, string name, string? description, Guid teacherId)
    {
        await session.RequireAsync(Management);
        var normalizedName = (name ?? "").Trim();
        if (normalizedName.Length < 3 || normalizedName.Length > 100)
            throw new ArgumentException("Informe um nome de disciplina entre 3 e 100 caracteres.");
        var normalizedDescription = (description ?? "").Trim();
        if (normalizedDescription.Length > 500)
            throw new ArgumentException("A descrição deve ter até 500 caracteres.");

        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.Users.AnyAsync(x => x.Id == teacherId && x.Ativa && x.Role == UserRole.Professor))
            throw new ArgumentException("Selecione um professor ativo.");
        if (await db.Disciplinas.AnyAsync(x => x.Ativa && x.Nome == normalizedName && x.Id != id))
            throw new ArgumentException("Já existe uma disciplina ativa com esse nome.");

        if (id is null)
        {
            db.Disciplinas.Add(new DisciplinaModel(normalizedName, normalizedDescription, teacherId));
        }
        else
        {
            var discipline = await db.Disciplinas.FirstOrDefaultAsync(x => x.Id == id && x.Ativa)
                ?? throw new InvalidOperationException("A disciplina não foi encontrada.");
            discipline.UpdateDisciplina(normalizedName, normalizedDescription, teacherId);
        }

        await db.SaveChangesAsync();
    }

    public async Task DisableDisciplineAsync(Guid id)
    {
        await session.RequireAsync(Management);
        await using var db = await dbFactory.CreateDbContextAsync();
        var discipline = await db.Disciplinas.FirstOrDefaultAsync(x => x.Id == id && x.Ativa)
            ?? throw new InvalidOperationException("A disciplina não foi encontrada.");
        discipline.Disable();
        await db.SaveChangesAsync();
    }

    // ───────────────────────── Pessoas ─────────────────────────

    public async Task<List<PersonRow>> GetStudentsAsync()
    {
        await session.RequireAsync(Teaching);
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Users.AsNoTracking()
            .Where(x => x.Ativa && x.Role == UserRole.Aluno)
            .OrderBy(x => x.Name)
            .Select(x => new PersonRow(x.Id, x.Name, x.Email, x.Telefone))
            .ToListAsync();
    }

    public async Task<List<StaffRow>> GetStaffAsync()
    {
        await session.RequireAsync(Management);
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Users.AsNoTracking()
            .Where(x => x.Ativa && (x.Role == UserRole.Professor || x.Role == UserRole.Gestor))
            .OrderBy(x => x.Name)
            .Select(x => new StaffRow(x.Id, x.Name, x.Email, x.Role))
            .ToListAsync();
    }

    public async Task<List<InstructorRow>> GetInstructorsAsync()
    {
        await session.RequireAsync(Management);
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Users.AsNoTracking().Where(x => x.Ativa && x.Role == UserRole.Professor)
            .OrderBy(x => x.Name)
            .Select(x => new InstructorRow(x.Id, x.Name))
            .ToListAsync();
    }

    // ───────────────────────── Turmas ─────────────────────────

    public async Task<List<ClassRow>> GetClassesAsync()
    {
        await session.RequireAsync(Management);
        await using var db = await dbFactory.CreateDbContextAsync();
        var enrolled = await db.Matriculas.AsNoTracking().Where(m => m.Ativa).Select(m => m.TurmaId).ToListAsync();
        var rows = await db.Turmas.AsNoTracking().Where(x => x.Ativo).OrderBy(x => x.Nome)
            .Select(x => new { x.Id, x.Nome, Curso = x.Curso.Nome, Professor = x.Professor.Name, x.Periodo })
            .ToListAsync();
        return rows.Select(x => new ClassRow(x.Id, x.Nome, x.Curso, x.Professor, x.Periodo, enrolled.Count(t => t == x.Id))).ToList();
    }

    public async Task CreateClassAsync(string name, Guid courseId, Guid instructorId)
    {
        await session.RequireAsync(Management);
        var normalizedName = (name ?? "").Trim();
        if (normalizedName.Length < 2 || normalizedName.Length > 150)
            throw new ArgumentException("Informe o nome da turma (até 150 caracteres).");

        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.Cursos.AnyAsync(x => x.Id == courseId && x.Ativo))
            throw new ArgumentException("Selecione um curso ativo.");
        if (!await db.Users.AnyAsync(x => x.Id == instructorId && x.Ativa && x.Role == UserRole.Professor))
            throw new ArgumentException("Selecione um professor ativo.");
        if (await db.Turmas.AnyAsync(x => x.Ativo && x.Nome == normalizedName))
            throw new ArgumentException("Já existe uma turma ativa com esse nome.");

        db.Turmas.Add(new TurmaModel(normalizedName, instructorId, courseId));
        await db.SaveChangesAsync();
    }

    // ───────────────────────── Matrículas ─────────────────────────

    public async Task<List<EnrollmentRow>> GetEnrollmentsAsync()
    {
        await session.RequireAsync(Management);
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Matriculas.AsNoTracking()
            .OrderByDescending(x => x.DataMatricula)
            .Select(x => new EnrollmentRow(x.Id, x.Usuario.Name, x.Turma.Nome, x.Turma.Curso.Nome, x.DataMatricula, x.Ativa))
            .ToListAsync();
    }

    public async Task CreateEnrollmentAsync(Guid studentId, Guid classId)
    {
        await session.RequireAsync(Management);
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.Users.AnyAsync(x => x.Id == studentId && x.Ativa && x.Role == UserRole.Aluno))
            throw new ArgumentException("Selecione um aluno ativo.");
        if (!await db.Turmas.AnyAsync(x => x.Id == classId && x.Ativo))
            throw new ArgumentException("Selecione uma turma ativa.");
        if (await db.Matriculas.AnyAsync(x => x.UsuarioId == studentId && x.TurmaId == classId && x.Ativa))
            throw new ArgumentException("Este aluno já está matriculado nesta turma.");

        db.Matriculas.Add(new MatriculaModel(studentId, classId));
        await db.SaveChangesAsync();
    }

    public async Task EndEnrollmentAsync(Guid id)
    {
        await session.RequireAsync(Management);
        await using var db = await dbFactory.CreateDbContextAsync();
        var enrollment = await db.Matriculas.FirstOrDefaultAsync(x => x.Id == id && x.Ativa)
            ?? throw new InvalidOperationException("A matrícula não foi encontrada.");
        enrollment.Disable();
        await db.SaveChangesAsync();
    }

    // ───────────────────────── Notas ─────────────────────────

    public async Task<List<GradeRow>> GetGradesAsync()
    {
        var actor = await session.RequireAsync(Teaching);
        await using var db = await dbFactory.CreateDbContextAsync();
        var actorId = actor.Id;
        var isTeacher = actor.Role == UserRole.Professor;

        var query = db.Notas.AsNoTracking().AsQueryable();
        if (isTeacher)
            query = query.Where(n => n.Matricula.Turma.ProfessorId == actorId);

        var rows = await query.OrderByDescending(n => n.DataLancamento)
            .Select(n => new
            {
                n.Id, n.DisciplinaId, n.UsuarioId, n.Descricao, n.Valor, n.DataLancamento,
                Aluno = n.Matricula.Usuario.Name, Turma = n.Matricula.Turma.Nome
            }).ToListAsync();

        var users = (await db.Users.AsNoTracking().Select(u => new { u.Id, u.Name }).ToListAsync())
            .ToDictionary(u => u.Id, u => u.Name);
        var disciplines = (await db.Disciplinas.AsNoTracking().Select(d => new { d.Id, d.Nome }).ToListAsync())
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
        await using var db = await dbFactory.CreateDbContextAsync();
        var actorId = actor.Id;
        var query = db.Matriculas.AsNoTracking().Where(x => x.Ativa);
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
        await using var db = await dbFactory.CreateDbContextAsync();
        var actorId = actor.Id;
        var query = db.Disciplinas.AsNoTracking().Where(x => x.Ativa);
        if (actor.Role == UserRole.Professor)
            query = query.Where(x => x.ProfessorId == actorId);
        return await query.OrderBy(x => x.Nome).Select(x => new DisciplineOption(x.Id, x.Nome)).ToListAsync();
    }

    public async Task CreateGradeAsync(Guid enrollmentId, Guid disciplineId, string description, decimal value)
    {
        var actor = await session.RequireAsync(Teaching);
        var text = (description ?? "").Trim();
        if (text.Length < 2 || text.Length > 120)
            throw new ArgumentException("Informe a avaliação (2 a 120 caracteres).");
        if (value < 0 || value > 10)
            throw new ArgumentException("A nota deve estar entre 0 e 10.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var actorId = actor.Id;
        var isTeacher = actor.Role == UserRole.Professor;

        var enrollmentQuery = db.Matriculas.Where(x => x.Id == enrollmentId && x.Ativa);
        if (isTeacher)
            enrollmentQuery = enrollmentQuery.Where(x => x.Turma.ProfessorId == actorId);
        if (!await enrollmentQuery.AnyAsync())
            throw new ArgumentException("Selecione uma matrícula ativa" + (isTeacher ? " de uma das suas turmas." : "."));

        var disciplineQuery = db.Disciplinas.Where(x => x.Id == disciplineId && x.Ativa);
        if (isTeacher)
            disciplineQuery = disciplineQuery.Where(x => x.ProfessorId == actorId);
        if (!await disciplineQuery.AnyAsync())
            throw new ArgumentException("Selecione uma disciplina ativa" + (isTeacher ? " sob sua responsabilidade." : "."));

        db.Notas.Add(new NotaModel(enrollmentId, actorId, disciplineId, text, decimal.Round(value, 2)));
        await db.SaveChangesAsync();
    }

    // ───────────────────────── Painel do aluno ─────────────────────────

    public async Task<StudentOverview> GetStudentOverviewAsync()
    {
        var actor = await session.RequireAsync(UserRole.Aluno);
        await using var db = await dbFactory.CreateDbContextAsync();
        var actorId = actor.Id;

        var enrollments = await db.Matriculas.AsNoTracking()
            .Where(m => m.UsuarioId == actorId)
            .OrderByDescending(m => m.DataMatricula)
            .Select(m => new MyEnrollmentRow(m.Turma.Nome, m.Turma.Curso.Nome, m.Turma.Professor.Name, m.DataMatricula, m.DataExpiracao, m.Ativa))
            .ToListAsync();

        var raw = await db.Notas.AsNoTracking()
            .Where(n => n.Matricula.UsuarioId == actorId)
            .OrderByDescending(n => n.DataLancamento)
            .Select(n => new { n.DisciplinaId, n.Descricao, n.Valor, n.DataLancamento, Turma = n.Matricula.Turma.Nome })
            .ToListAsync();
        var disciplines = (await db.Disciplinas.AsNoTracking().Select(d => new { d.Id, d.Nome }).ToListAsync())
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
        await using var db = await dbFactory.CreateDbContextAsync();
        var actorId = actor.Id;

        var classes = await db.Turmas.AsNoTracking().Where(t => t.Ativo && t.ProfessorId == actorId)
            .OrderBy(t => t.Nome)
            .Select(t => new { t.Id, t.Nome, Curso = t.Curso.Nome, t.Periodo }).ToListAsync();
        var enrolled = await db.Matriculas.AsNoTracking()
            .Where(m => m.Ativa && m.Turma.ProfessorId == actorId)
            .Select(m => new { m.TurmaId, m.UsuarioId }).ToListAsync();
        var disciplineCount = await db.Disciplinas.CountAsync(d => d.Ativa && d.ProfessorId == actorId);
        var gradeCount = await db.Notas.CountAsync(n => n.UsuarioId == actorId);

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
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Contatos.AsNoTracking().OrderByDescending(c => c.CriadoEm)
            .Select(c => new ContactRow(c.Id, c.Nome, c.Email, c.Telefone, c.Instituicao, c.Papel, c.Mensagem, c.CriadoEm, c.Lido))
            .ToListAsync();
    }

    public async Task MarkContactReadAsync(Guid id)
    {
        await session.RequireAsync(Management);
        await using var db = await dbFactory.CreateDbContextAsync();
        var contact = await db.Contatos.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("A solicitação não foi encontrada.");
        contact.MarcarComoLido();
        await db.SaveChangesAsync();
    }
}
