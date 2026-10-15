using EduAssess.Data;
using EduAssess.Models;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Web;

public sealed class AcademicDashboardService(AppDbContext db, UiSession session)
{
    public async Task<DashboardSummary> GetDashboardAsync()
    {
        EnsureSignedIn();
        var recentEnrollments = await db.Matriculas
            .AsNoTracking()
            .OrderByDescending(x => x.DataMatricula)
            .Take(5)
            .Select(x => new EnrollmentRow(
                x.Id,
                x.Usuario.Name,
                x.Turma.Nome,
                x.Turma.Curso.Nome,
                x.DataMatricula,
                x.Ativa))
            .ToListAsync();

        return new DashboardSummary(
            await db.Cursos.CountAsync(x => x.Ativo),
            await db.Turmas.CountAsync(x => x.Ativo),
            await db.Users.CountAsync(x => x.Ativa),
            await db.Matriculas.CountAsync(x => x.Ativa),
            recentEnrollments);
    }

    public async Task<List<CourseRow>> GetCoursesAsync()
    {
        EnsureSignedIn();
        return await db.Cursos.AsNoTracking().Where(x => x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new CourseRow(x.Id, x.Nome, x.Descricao, x.DuracaoEmAnos))
            .ToListAsync();
    }

    public async Task SaveCourseAsync(Guid? id, string name, string description, int duration)
    {
        session.RequireManagement();
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 3)
            throw new ArgumentException("Informe um nome de curso com pelo menos 3 caracteres.");
        if (duration < 1)
            throw new ArgumentException("A duração deve ser de pelo menos 1 ano.");

        var normalizedName = name.Trim();
        if (await db.Cursos.AnyAsync(x => x.Ativo && x.Nome == normalizedName && x.Id != id))
            throw new ArgumentException("Já existe um curso ativo com esse nome.");

        if (id is null)
        {
            db.Cursos.Add(new CursoModel(normalizedName, description.Trim(), duration));
        }
        else
        {
            var course = await db.Cursos.FirstOrDefaultAsync(x => x.Id == id && x.Ativo)
                ?? throw new InvalidOperationException("O curso não foi encontrado.");
            course.Update(normalizedName, description.Trim(), duration);
        }

        await db.SaveChangesAsync();
    }

    public async Task DisableCourseAsync(Guid id)
    {
        session.RequireManagement();
        var course = await db.Cursos.FirstOrDefaultAsync(x => x.Id == id && x.Ativo)
            ?? throw new InvalidOperationException("O curso não foi encontrado.");
        course.Disable();
        await db.SaveChangesAsync();
    }

    public async Task<List<PersonRow>> GetStudentsAsync()
    {
        EnsureSignedIn();
        return await db.Users.AsNoTracking()
            .Where(x => x.Ativa && x.Role == UserRole.Aluno)
            .OrderBy(x => x.Name)
            .Select(x => new PersonRow(x.Id, x.Name, x.Email, x.Telefone))
            .ToListAsync();
    }

    public async Task CreateStudentAsync(string name, string email, string cpf, string phone, DateOnly birthDate, string password)
    {
        session.RequireManagement();
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 3)
            throw new ArgumentException("Informe um nome com pelo menos 3 caracteres.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("Informe um e-mail válido.");
        if (cpf.Length != 11 || !cpf.All(char.IsDigit))
            throw new ArgumentException("O CPF deve conter 11 números.");
        if (phone.Length is < 10 or > 11 || !phone.All(char.IsDigit))
            throw new ArgumentException("O telefone deve conter 10 ou 11 números.");
        if (password.Length < 8)
            throw new ArgumentException("A senha deve ter no mínimo 8 caracteres.");
        if (await db.Users.AnyAsync(x => x.Email == email.Trim()))
            throw new ArgumentException("Já existe um usuário com esse e-mail.");

        db.Users.Add(new UserModel(name.Trim(), email.Trim(), cpf, phone, birthDate, password, UserRole.Aluno));
        await db.SaveChangesAsync();
    }

    public async Task<List<InstructorRow>> GetInstructorsAsync()
    {
        EnsureSignedIn();
        return await db.Users.AsNoTracking().Where(x => x.Ativa && x.Role == UserRole.Professor)
            .OrderBy(x => x.Name)
            .Select(x => new InstructorRow(x.Id, x.Name))
            .ToListAsync();
    }

    public async Task CreateInstructorAsync(string name, string email, string cpf, string phone, DateOnly birthDate, string password)
    {
        session.RequireManagement();
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 3)
            throw new ArgumentException("Informe um nome com pelo menos 3 caracteres.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("Informe um e-mail válido.");
        if (cpf.Length != 11 || !cpf.All(char.IsDigit))
            throw new ArgumentException("O CPF deve conter 11 números.");
        if (phone.Length is < 10 or > 11 || !phone.All(char.IsDigit))
            throw new ArgumentException("O telefone deve conter 10 ou 11 números.");
        if (password.Length < 8)
            throw new ArgumentException("A senha deve ter no mínimo 8 caracteres.");
        if (await db.Users.AnyAsync(x => x.Email == email.Trim()))
            throw new ArgumentException("Já existe um usuário com esse e-mail.");

        db.Users.Add(new UserModel(name.Trim(), email.Trim(), cpf, phone, birthDate, password, UserRole.Professor));
        await db.SaveChangesAsync();
    }

    public async Task<List<ClassRow>> GetClassesAsync()
    {
        EnsureSignedIn();
        return await db.Turmas.AsNoTracking().Where(x => x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new ClassRow(x.Id, x.Nome, x.Curso.Nome, x.Professor.Name, x.Periodo))
            .ToListAsync();
    }

    public async Task CreateClassAsync(string name, Guid courseId, Guid instructorId)
    {
        session.RequireManagement();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Informe o nome da turma.");
        var courseExists = await db.Cursos.AnyAsync(x => x.Id == courseId && x.Ativo);
        if (!courseExists)
            throw new ArgumentException("Selecione um curso ativo.");
        var instructorExists = await db.Users.AnyAsync(x => x.Id == instructorId && x.Ativa && x.Role == UserRole.Professor);
        if (!instructorExists)
            throw new ArgumentException("Selecione um professor ativo.");
        if (await db.Turmas.AnyAsync(x => x.Ativo && x.Nome == name.Trim()))
            throw new ArgumentException("Já existe uma turma ativa com esse nome.");

        db.Turmas.Add(new TurmaModel(name.Trim(), instructorId, courseId));
        await db.SaveChangesAsync();
    }

    public async Task<List<EnrollmentRow>> GetEnrollmentsAsync()
    {
        EnsureSignedIn();
        return await db.Matriculas.AsNoTracking()
            .OrderByDescending(x => x.DataMatricula)
            .Select(x => new EnrollmentRow(
                x.Id,
                x.Usuario.Name,
                x.Turma.Nome,
                x.Turma.Curso.Nome,
                x.DataMatricula,
                x.Ativa))
            .ToListAsync();
    }

    public async Task CreateEnrollmentAsync(Guid studentId, Guid classId)
    {
        session.RequireManagement();
        if (!await db.Users.AnyAsync(x => x.Id == studentId && x.Ativa && x.Role == UserRole.Aluno))
            throw new ArgumentException("Selecione um aluno ativo.");
        if (!await db.Turmas.AnyAsync(x => x.Id == classId && x.Ativo))
            throw new ArgumentException("Selecione uma turma ativa.");
        if (await db.Matriculas.AnyAsync(x => x.UsuarioId == studentId && x.TurmaId == classId && x.Ativa))
            throw new ArgumentException("Este aluno já está matriculado nesta turma.");

        db.Matriculas.Add(new MatriculaModel(studentId, classId));
        await db.SaveChangesAsync();
    }

    public async Task<List<GradeRow>> GetGradesAsync()
    {
        EnsureSignedIn();
        return await db.Notas.AsNoTracking()
            .OrderByDescending(x => x.DataLancamento)
            .Select(x => new GradeRow(
                x.Id,
                x.Matricula.Usuario.Name,
                x.Matricula.Turma.Nome,
                x.Descricao,
                x.Valor,
                x.DataLancamento))
            .ToListAsync();
    }

    public async Task<List<EnrollmentOption>> GetEnrollmentOptionsAsync()
    {
        EnsureSignedIn();
        return await db.Matriculas.AsNoTracking().Where(x => x.Ativa)
            .OrderBy(x => x.Usuario.Name)
            .Select(x => new EnrollmentOption(x.Id, x.Usuario.Name + " — " + x.Turma.Nome))
            .ToListAsync();
    }

    public async Task CreateGradeAsync(Guid enrollmentId, string description, decimal value)
    {
        session.RequireManagement();
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Informe a descrição da avaliação.");

        var enrollment = await db.Matriculas
            .FirstOrDefaultAsync(x => x.Id == enrollmentId && x.Ativa)
            ?? throw new ArgumentException("Selecione uma matrícula ativa.");

        db.Notas.Add(new NotaModel(enrollmentId, enrollment.UsuarioId, description.Trim(), value));
        await db.SaveChangesAsync();
    }

    private void EnsureSignedIn()
    {
        if (!session.IsSignedIn)
            throw new UnauthorizedAccessException("Faça login para visualizar os dados acadêmicos.");
    }
}

public sealed record DashboardSummary(int CourseCount, int ClassCount, int StudentCount, int EnrollmentCount, List<EnrollmentRow> RecentEnrollments);
public sealed record CourseRow(Guid Id, string Name, string Description, int Duration);
public sealed record PersonRow(Guid Id, string Name, string Email, string Phone);
public sealed record InstructorRow(Guid Id, string Name);
public sealed record ClassRow(Guid Id, string Name, string CourseName, string InstructorName, int Period);
public sealed record EnrollmentRow(Guid Id, string StudentName, string ClassName, string CourseName, DateTime EnrolledAt, bool Active);
public sealed record GradeRow(Guid Id, string StudentName, string ClassName, string Description, decimal Value, DateTime CreatedAt);
public sealed record EnrollmentOption(Guid Id, string Label);
