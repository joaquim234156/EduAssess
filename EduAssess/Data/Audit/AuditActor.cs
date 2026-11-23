using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace EduAssess.Data.Audit;

/// <summary>Quem está executando a operação (gravado em cada registro de auditoria).</summary>
public sealed record AuditActor(Guid? UserId, string Name, string Role, Guid? InstitutionId, string? Ip);

public interface IAuditActorAccessor
{
    AuditActor? Current { get; }
    void Set(AuditActor? actor);
}

/// <summary>
/// Escopo por circuito Blazor / por requisição. No painel, o UiSession informa o ator com Set(). Na API
/// (JWT ou cookie), o ator é lido das claims da requisição. Sem ator = operação do sistema.
/// </summary>
public sealed class AuditActorAccessor(IHttpContextAccessor http) : IAuditActorAccessor
{
    public const string InstitutionClaim = "eduassess:institution_id";
    private AuditActor? explicitActor;

    public AuditActor? Current => explicitActor ?? FromHttp();

    public void Set(AuditActor? actor) => explicitActor = actor;

    // Perfil legível ("Administrador"), igual ao do painel; se não reconhecer, guarda como veio.
    private static string RoleLabel(string? role) =>
        Enum.TryParse<EduAssess.Models.Local.UserRole>(role, out var r) ? EduAssess.Web.UiFormat.RoleLabel(r) : role ?? "";

    private AuditActor? FromHttp()
    {
        var context = http.HttpContext;
        var user = context?.User;
        if (user?.Identity?.IsAuthenticated != true) return null;
        Guid? id = Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g) ? g : null;
        Guid? inst = Guid.TryParse(user.FindFirst(InstitutionClaim)?.Value, out var i) ? i : null;
        return new AuditActor(id, user.FindFirst(ClaimTypes.Name)?.Value ?? user.FindFirst(ClaimTypes.Email)?.Value ?? "Usuário",
            RoleLabel(user.FindFirst(ClaimTypes.Role)?.Value), inst, context?.Connection.RemoteIpAddress?.ToString());
    }
}
