using EduAssess.Data;
using EduAssess.Models;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Web;

public sealed class UiSession(AppDbContext db)
{
    public UserSummary? User { get; private set; }
    public bool IsSignedIn => User is not null;
    public bool CanManage => User?.Role is UserRole.Admin or UserRole.Gestor;
    public bool CanCreateManager => User?.Role == UserRole.Admin;
    public bool CanCreateProfessor => User?.Role == UserRole.Gestor;
    public bool CanAssignStudents => User?.Role is UserRole.Admin or UserRole.Gestor or UserRole.Professor;

    public event Action? Changed;

    public async Task<bool> SignInAsync(string email, string password)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail && x.Ativa);
        if (user is null
            || !user.VerifyPassword(password)

            || user.Role is not (UserRole.Admin or UserRole.Gestor or UserRole.Professor or UserRole.Aluno))

            return false;

        User = new UserSummary(user.Id, user.Name, user.Email, user.Role);
        Changed?.Invoke();
        return true;
    }

    public async Task RefreshAuthorizationAsync()
    {
        if (User is null)
            return;

        var user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == User.Id);
        if (user is null
            || !user.Ativa
            || user.Role is not (UserRole.Admin or UserRole.Gestor or UserRole.Professor))
        {
            SignOut();
            return;
        }

        var refreshedUser = new UserSummary(user.Id, user.Name, user.Email, user.Role);
        if (refreshedUser != User)
        {
            User = refreshedUser;
            Changed?.Invoke();
        }
    }

    public void SignOut()
    {
        User = null;
        Changed?.Invoke();
    }

    public async Task RequireManagementAsync()
    {
        await RefreshAuthorizationAsync();
        if (!CanManage)
            throw new UnauthorizedAccessException("Esta ação exige perfil de Admin ou Gestor.");
    }

    public async Task RequireStudentAssignmentAsync()
    {
        await RefreshAuthorizationAsync();
        if (!CanAssignStudents)
            throw new UnauthorizedAccessException("Esta ação exige perfil de Admin, Gestor ou Professor.");
    }

}

public sealed record UserSummary(Guid Id, string Name, string Email, UserRole Role);
