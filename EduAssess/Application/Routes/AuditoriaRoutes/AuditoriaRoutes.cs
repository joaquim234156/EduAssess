using System.Security.Claims;
using EduAssess.Application.DTOs.Auditoria;
using EduAssess.Application.Services;
using EduAssess.Data;
using EduAssess.Models.Global;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Routes
{
    /// <summary>
    /// NOVO: histórico de atividades pela API. SOMENTE LEITURA: só existem GETs; não há POST/PUT/DELETE
    /// para registros de auditoria. Rota: GET /auditoria/list (o caminho exato /auditoria é página do painel). O escopo por perfil é aplicado no AuditoriaService (backend).
    /// </summary>
    public static class AuditoriaRoute
    {
        public static void AuditoriaRoutes(this WebApplication app)
        {
            var route = app.MapGroup("auditoria").WithTags("Auditoria");

            route.MapGet("list", async (ClaimsPrincipal principal, AuditoriaService audit, AppDbContext db,
                DateTime? de, DateTime? ate, string? acao, string? categoria, string? entidade, string? busca,
                Guid? instituicaoId, bool? somenteAlunos, int? pagina, int? tamanho, CancellationToken ct) =>
            {
                var viewer = await BuildViewerAsync(principal, db, instituicaoId, ct);
                if (viewer is null) return Results.Forbid();

                var filter = new AuditFilter
                {
                    From = de, To = ate?.Date.AddDays(1), Entidade = entidade, Search = busca, InstituicaoId = instituicaoId,
                    OnlyStudentActivity = somenteAlunos == true, Page = pagina ?? 1, PageSize = tamanho ?? 25,
                    Acao = Enum.TryParse<AcaoAuditoria>(acao, true, out var a) ? a : null,
                    Categoria = Enum.TryParse<CategoriaAuditoria>(categoria, true, out var c) ? c : null
                };
                try { return Results.Ok(await audit.ListarAsync(viewer, filter, ct)); }
                catch (UnauthorizedAccessException) { return Results.Forbid(); }
            })
            .RequireAuthorization("Docencia")
            .WithSummary("Lista o histórico de atividades que o perfil do usuário pode ver (somente leitura)");

            route.MapGet("atividade-alunos", async (ClaimsPrincipal principal, AuditoriaService audit, AppDbContext db,
                DateTime? de, DateTime? ate, CancellationToken ct) =>
            {
                var viewer = await BuildViewerAsync(principal, db, null, ct);
                if (viewer is null) return Results.Forbid();
                try { return Results.Ok(await audit.ResumoAtividadeAsync(viewer, de, ate?.Date.AddDays(1), ct)); }
                catch (UnauthorizedAccessException) { return Results.Forbid(); }
            })
            .RequireAuthorization("Docencia")
            .WithSummary("Resumo da atividade dos alunos nas questões e testes (Admin e Professor)");
        }

        /// <summary>
        /// Perfil vem da claim do token/cookie; instituições vêm do banco (vínculos ativos), nunca do cliente.
        /// Admin vê todas (ou a pedida); Gestor e Professor, só as instituições em que atuam.
        /// </summary>
        private static async Task<AuditViewer?> BuildViewerAsync(ClaimsPrincipal principal, AppDbContext db, Guid? requested, CancellationToken ct)
        {
            if (!Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)) return null;
            var role = principal.IsInRole(nameof(UserRole.Admin)) ? UserRole.Admin
                : principal.IsInRole(nameof(UserRole.Gestor)) ? UserRole.Gestor
                : principal.IsInRole(nameof(UserRole.Professor)) ? UserRole.Professor : UserRole.User;
            if (role == UserRole.User) return null;
            if (role == UserRole.Admin) return new AuditViewer(userId, role, null);

            var mine = await db.UsuariosInstituicoes.AsNoTracking()
                .Where(ui => ui.UsuarioId == userId && ui.Ativa).Select(ui => ui.InstituicaoId).ToListAsync(ct);
            if (requested is Guid r) mine = mine.Where(x => x == r).ToList();
            return new AuditViewer(userId, role, mine);
        }
    }
}
