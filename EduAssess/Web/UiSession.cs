using EduAssess.Data;
using EduAssess.Models;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Web;

public sealed class UiSession(AppDbContext db)
{
    public UserSummary? User { get; private set; }
    public bool IsSignedIn => User is not null;
    public bool CanManage => User?.Role is UserRole.Admin or UserRole.Gestor;

    public event Action? Changed;

    public async Task<bool> SignInAsync(string email, string password)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email && x.Ativa);
        if (user is null
            || !user.VerifyPassword(password)
            || user.Role is not (UserRole.Admin or UserRole.Gestor))
            return false;

        User = new UserSummary(user.Id, user.Name, user.Email, user.Role);
        Changed?.Invoke();
        return true;
    }

    public void SignOut()
    {
        User = null;
        Changed?.Invoke();
    }

    public void RequireManagement()
    {
        if (!CanManage)
            throw new UnauthorizedAccessException("Esta ação exige perfil de Admin ou Gestor.");
    }
}

public sealed record UserSummary(Guid Id, string Name, string Email, UserRole Role);
