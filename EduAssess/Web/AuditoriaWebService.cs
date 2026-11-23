using EduAssess.Application.DTOs.Auditoria;
using EduAssess.Application.Services;
using EduAssess.Models.Local;

namespace EduAssess.Web;

/// <summary>
/// NOVO: ponte entre a tela /historico e o AuditoriaService. A verificação de perfil acontece AQUI e de novo no
/// AuditoriaService (não depende de esconder botões). Só leitura: não há método de alterar ou excluir.
/// </summary>
public sealed class AuditoriaWebService(UiSession session, AuditoriaService audit)
{
    private static readonly UserRole[] Allowed = [UserRole.Admin, UserRole.Gestor, UserRole.Professor];

    private async Task<AuditViewer> ViewerAsync()
    {
        var me = await session.RequireAsync(Allowed);
        // Admin enxerga todas as instituições; Gestor e Professor, só a instituição em uso.
        IReadOnlyCollection<Guid>? ids = me.Role == UserRole.Admin
            ? null
            : [session.InstitutionId ?? throw new UnauthorizedAccessException("Selecione uma instituição.")];
        return new AuditViewer(me.Id, me.Role, ids);
    }

    public async Task<AuditPageDto> ListAsync(AuditFilter filter) => await audit.ListarAsync(await ViewerAsync(), filter);

    public async Task<StudentActivitySummary> StudentActivityAsync(DateTime? from, DateTime? to) =>
        await audit.ResumoAtividadeAsync(await ViewerAsync(), from, to);
}
