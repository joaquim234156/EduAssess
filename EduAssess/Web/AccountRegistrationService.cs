using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text;
using EduAssess.Data;
using EduAssess.Models;
using EduAssess.Models.Global;
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
        string password, string confirmPassword, CancellationToken cancellationToken = default,
        Guid? institutionId = null, Guid? unitId = null)
    {
        if (actorRole != UserRole.Admin)
            throw new UnauthorizedAccessException("Somente Admin pode criar gestores.");

        // ALTERADO: institutionId/unitId são opcionais. A tela de Equipe passa a instituição em uso para a conta já
        // nascer vinculada (sem vínculo, o login transformava o gestor em "User" e ele ficava sem tela).
        return CreateAsync(name, email, cpf, phone, birthDate, password, confirmPassword, UserRole.Gestor,
            cancellationToken, institutionId, unitId);
    }

    public Task<UserModel> CreateProfessorAsync(
        UserRole actorRole, string name, string email, string cpf, string phone, DateOnly birthDate,
        string password, string confirmPassword, CancellationToken cancellationToken = default,
        Guid? institutionId = null, Guid? unitId = null)
    {
        if (actorRole != UserRole.Gestor)
            throw new UnauthorizedAccessException("Somente Gestor pode criar professores.");

        // ALTERADO: mesma correção do gestor — o professor nasce vinculado à instituição em uso.
        return CreateAsync(name, email, cpf, phone, birthDate, password, confirmPassword, UserRole.Professor,
            cancellationToken, institutionId, unitId);
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

    // ALTERADO: agora recebe a instituição (e a unidade) em uso. Antes só trocava o perfil para Aluno e NÃO criava
    // vínculo: o aluno não pertencia a nenhuma instituição, não aparecia em "Alunos ativos" nem na lista de
    // matrícula da instituição de quem atribuiu (tudo isso lê a tabela de vínculos).
    public async Task AssignAsStudentAsync(
        UserRole actorRole, Guid userId, Guid institutionId, Guid? unitId = null,
        CancellationToken cancellationToken = default)
    {
        EnsureCanAssignStudent(actorRole);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.Ativa, cancellationToken)
            ?? throw new KeyNotFoundException("A conta não foi encontrada.");

        if (!await db.Instituicoes.AnyAsync(i => i.Id == institutionId && i.Ativa, cancellationToken))
            throw new InvalidOperationException("A instituição em uso não existe ou está inativa.");
        if (unitId is Guid uId
            && !await db.Unidades.AnyAsync(u => u.Id == uId && u.InstituicaoId == institutionId && u.Ativa, cancellationToken))
            unitId = null; // unidade de outra instituição não vale: o aluno entra na instituição, sem prédio

        user.AssignAsStudent();

        // NOVO: vínculo de aluno na instituição de quem atribuiu (reativa se já existia um vínculo antigo).
        var link = await db.UsuariosInstituicoes
            .FirstOrDefaultAsync(x => x.UsuarioId == userId && x.InstituicaoId == institutionId, cancellationToken);
        if (link is null)
            db.UsuariosInstituicoes.Add(new UsuarioInstituicaoModel(userId, institutionId, PapelInstituicao.Aluno, unitId));
        else
            link.Reativar(PapelInstituicao.Aluno, unitId);

        // Conta + perfil + vínculo salvam juntos (tudo ou nada).
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserModel> CreateAsync(
        string name, string email, string cpf, string phone, DateOnly birthDate,
        string password, string confirmPassword, UserRole role, CancellationToken cancellationToken,
        Guid? institutionId = null, Guid? unitId = null)
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

        // NOVO: confere a instituição (e a unidade, se vier) ANTES de criar a conta, para não deixar conta órfã.
        if (institutionId is Guid instId)
        {
            if (!await db.Instituicoes.AnyAsync(i => i.Id == instId && i.Ativa, cancellationToken))
                throw new ArgumentException("A instituição informada não existe ou está inativa.");
            if (unitId is Guid uId
                && !await db.Unidades.AnyAsync(u => u.Id == uId && u.InstituicaoId == instId && u.Ativa, cancellationToken))
                throw new ArgumentException("A unidade informada não existe, está inativa ou não pertence a esta instituição.");
        }

        var user = new UserModel(name.Trim(), normalizedEmail, cpf, phone, birthDate, password, role);
        db.Users.Add(user);

        // NOVO: o vínculo é salvo junto com a conta (um único SaveChanges = tudo ou nada). O papel do vínculo
        // segue o perfil: Gestor -> Gestor, Professor -> Professor. Conta comum (User) não ganha vínculo.
        if (institutionId is Guid linkInstitution && (role is UserRole.Gestor or UserRole.Professor))
        {
            var papel = role == UserRole.Gestor ? PapelInstituicao.Gestor : PapelInstituicao.Professor;
            db.UsuariosInstituicoes.Add(new UsuarioInstituicaoModel(user.Id, linkInstitution, papel, unitId));
        }

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

    // NOVO: limite de tentativas erradas por e-mail (anti "chute" de telefone). Fica em memória: reinicia com o app.
    private const int MaxResetFailures = 5;
    private static readonly TimeSpan ResetWindow = TimeSpan.FromMinutes(15);
    private static readonly ConcurrentDictionary<string, (int Failures, DateTime WindowStart)> resetAttempts = new();

    /// <summary>
    /// NOVO: "Esqueci minha senha". POR ENQUANTO a pessoa prova que é ela informando e-mail + telefone cadastrados.
    /// NO FUTURO (quando houver envio de e-mail): troque só a checagem do telefone, abaixo, por um token enviado por
    /// e-mail (gerar token com validade, enviar o link, validar o token aqui). O resto do fluxo continua igual.
    /// </summary>
    public async Task ResetPasswordWithPhoneAsync(
        string email, string phone, string newPassword, string confirmPassword,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? "").Trim().ToLowerInvariant();
        var phoneDigits = UiFormat.Digits(phone ?? "");

        // Mensagem única para "e-mail não existe" e "telefone não confere": não revela quais e-mails têm conta.
        const string notConfirmed = "Não foi possível confirmar os dados. Confira o e-mail e o telefone cadastrados.";

        if (string.IsNullOrWhiteSpace(normalizedEmail) || normalizedEmail.Length > 150)
            throw new ArgumentException(notConfirmed);

        // Limite de tentativas: 5 erros em 15 minutos bloqueiam novas tentativas para aquele e-mail.
        var now = DateTime.UtcNow;
        if (resetAttempts.TryGetValue(normalizedEmail, out var state))
        {
            if (now - state.WindowStart > ResetWindow) resetAttempts.TryRemove(normalizedEmail, out _);
            else if (state.Failures >= MaxResetFailures)
                throw new InvalidOperationException("Muitas tentativas. Aguarde 15 minutos e tente de novo.");
        }

        // A nova senha segue as mesmas regras do cadastro (mín. 8 caracteres, máx. 72 bytes).
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8 || Encoding.UTF8.GetByteCount(newPassword) > 72)
            throw new ArgumentException("A senha deve ter pelo menos 8 caracteres e no máximo 72 bytes.");
        if (newPassword != confirmPassword)
            throw new ArgumentException("A senha e a confirmação não coincidem.");

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail && x.Ativa, cancellationToken);

        // Conta de Admin NÃO pode ser redefinida só com telefone (telefone é fácil de descobrir; é a conta mais poderosa).
        var confirmed = user is not null
            && user.Role != UserRole.Admin
            && UiFormat.Digits(user.Telefone) == phoneDigits
            && phoneDigits.Length is >= 10 and <= 11;

        if (!confirmed)
        {
            resetAttempts.AddOrUpdate(normalizedEmail,
                _ => (1, now),
                (_, old) => now - old.WindowStart > ResetWindow ? (1, now) : (old.Failures + 1, old.WindowStart));
            throw new ArgumentException(notConfirmed);
        }

        user!.UpdatePassword(newPassword, confirmPassword);
        await db.SaveChangesAsync(cancellationToken);
        resetAttempts.TryRemove(normalizedEmail, out _);
    }

    private static void EnsureCanAssignStudent(UserRole actorRole)
    {
        if (actorRole is not (UserRole.Admin or UserRole.Gestor or UserRole.Professor))
            throw new UnauthorizedAccessException("Somente Admin, Gestor ou Professor pode atribuir o perfil de aluno.");
    }
}

public sealed class AccountConflictException(string message) : Exception(message);
