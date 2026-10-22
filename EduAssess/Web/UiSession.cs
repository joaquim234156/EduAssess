using System.Security.Claims;
using EduAssess.Data;
using EduAssess.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Web;

/// <summary>
/// Sessão do painel. A identidade vem do cookie de autenticação (sobrevive a F5 e a novas abas);
/// cada operação protegida revalida no banco se a conta continua ativa e qual é o perfil atual.
/// </summary>
public sealed class UiSession(AuthenticationStateProvider authenticationStateProvider, IDbContextFactory<AppDbContext> dbFactory)
{
    private bool initialized;

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
        }

        initialized = true;
    }

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

        User = new UserSummary(userId, row.Name, row.Email, row.Role);

        if (allowedRoles.Length > 0 && !allowedRoles.Contains(row.Role))
            throw new UnauthorizedAccessException("Seu perfil não tem permissão para esta ação.");

        return User;
    }
}

public sealed record UserSummary(Guid Id, string Name, string Email, UserRole Role);
