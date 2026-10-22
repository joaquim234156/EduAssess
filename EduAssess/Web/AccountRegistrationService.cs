using System.ComponentModel.DataAnnotations;
using System.Text;
using EduAssess.Data;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Web;

public sealed class AccountRegistrationService(IDbContextFactory<AppDbContext> dbFactory)
{
    public Task<UserModel> RegisterUserAsync(
        string name, string email, string cpf, string phone, DateOnly birthDate,
        string password, string confirmPassword, CancellationToken cancellationToken = default) =>
        CreateAsync(name, email, cpf, phone, birthDate, password, confirmPassword, UserRole.User, cancellationToken);

    public Task<UserModel> CreateManagerAsync(
        UserRole actorRole, string name, string email, string cpf, string phone, DateOnly birthDate,
        string password, string confirmPassword, CancellationToken cancellationToken = default)
    {
        if (actorRole != UserRole.Admin)
            throw new UnauthorizedAccessException("Somente Admin pode criar gestores.");

        return CreateAsync(name, email, cpf, phone, birthDate, password, confirmPassword, UserRole.Gestor, cancellationToken);
    }

    public Task<UserModel> CreateProfessorAsync(
        UserRole actorRole, string name, string email, string cpf, string phone, DateOnly birthDate,
        string password, string confirmPassword, CancellationToken cancellationToken = default)
    {
        if (actorRole != UserRole.Gestor)
            throw new UnauthorizedAccessException("Somente Gestor pode criar professores.");

        return CreateAsync(name, email, cpf, phone, birthDate, password, confirmPassword, UserRole.Professor, cancellationToken);
    }

    public async Task<List<PersonRow>> GetPendingStudentAssignmentsAsync(
        UserRole actorRole, CancellationToken cancellationToken = default)
    {
        EnsureCanAssignStudent(actorRole);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Users.AsNoTracking()
            .Where(x => x.Ativa && x.Role == UserRole.User)
            .OrderBy(x => x.Name)
            .Select(x => new PersonRow(x.Id, x.Name, x.Email, x.Telefone))
            .ToListAsync(cancellationToken);
    }

    public async Task AssignAsStudentAsync(
        UserRole actorRole, Guid userId, CancellationToken cancellationToken = default)
    {
        EnsureCanAssignStudent(actorRole);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.Ativa, cancellationToken)
            ?? throw new KeyNotFoundException("A conta não foi encontrada.");

        user.AssignAsStudent();
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserModel> CreateAsync(
        string name, string email, string cpf, string phone, DateOnly birthDate,
        string password, string confirmPassword, UserRole role, CancellationToken cancellationToken)
    {
        cpf = UiFormat.Digits(cpf);
        phone = UiFormat.Digits(phone);
        Validate(name, email, cpf, phone, birthDate, password, confirmPassword);

        var normalizedEmail = email.Trim().ToLowerInvariant();
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Users.AnyAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken))
            throw new AccountConflictException("Já existe uma conta com esse e-mail.");
        if (await db.Users.AnyAsync(x => x.Cpf == cpf, cancellationToken))
            throw new AccountConflictException("Já existe uma conta com esse CPF.");

        var user = new UserModel(name.Trim(), normalizedEmail, cpf, phone, birthDate, password, role);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static void Validate(
        string name, string email, string cpf, string phone, DateOnly birthDate,
        string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length is < 3 or > 100)
            throw new ArgumentException("O nome deve ter entre 3 e 100 caracteres.");
        if (string.IsNullOrWhiteSpace(email) || email.Trim().Length > 150
            || !new EmailAddressAttribute().IsValid(email.Trim()))
            throw new ArgumentException("Informe um e-mail válido com até 150 caracteres.");
        if (cpf.Length != 11 || !cpf.All(char.IsDigit))
            throw new ArgumentException("O CPF deve conter exatamente 11 números.");
        if (phone.Length is < 10 or > 11 || !phone.All(char.IsDigit))
            throw new ArgumentException("O telefone deve conter 10 ou 11 números.");
        if (birthDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("A data de nascimento não pode estar no futuro.");
        if (string.IsNullOrEmpty(password) || password.Length < 8 || Encoding.UTF8.GetByteCount(password) > 72)
            throw new ArgumentException("A senha deve ter pelo menos 8 caracteres e no máximo 72 bytes.");
        if (password != confirmPassword)
            throw new ArgumentException("A senha e a confirmação não coincidem.");
    }

    private static void EnsureCanAssignStudent(UserRole actorRole)
    {
        if (actorRole is not (UserRole.Admin or UserRole.Gestor or UserRole.Professor))
            throw new UnauthorizedAccessException("Somente Admin, Gestor ou Professor pode atribuir o perfil de aluno.");
    }
}

public sealed class AccountConflictException(string message) : Exception(message);
