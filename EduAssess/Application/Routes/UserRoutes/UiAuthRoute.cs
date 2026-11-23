using System.Security.Claims;
using EduAssess.Application.Services;
using EduAssess.Data;
using EduAssess.Data.Audit;
using EduAssess.Models.Global;
using EduAssess.Models.Local;
using EduAssess.Models;
using EduAssess.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Application.Routes
{
    /// <summary>
    /// Login/logout do painel web. O cookie de autenticação só pode ser emitido por uma requisição HTTP
    /// (não por dentro do circuito Blazor), então o formulário de login faz um POST nativo para estas rotas.
    /// </summary>
    public static class UiAuthRoute
    {
        // Hash usado só para igualar o tempo de resposta quando o e-mail não existe.
        private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("eduassess-dummy-password");

        public static void UiAuthRoutes(this WebApplication app)
        {
            app.MapPost("/ui/login", async (HttpContext http, IDbContextFactory<AppDbContext> dbFactory, AuditoriaService audit) =>
            {
                var form = await http.Request.ReadFormAsync();
                var email = form["email"].ToString().Trim().ToLowerInvariant();
                var password = form["password"].ToString();
                var returnUrl = form["returnUrl"].ToString();
                var remember = form["remember"].ToString() == "on";
                // NOVO: prédio escolhido na tela de login; é devolvido na URL de erro para a tela manter as cores dele.
                var unidade = UiTheme.NormalizeLoginUnit(form["unidade"].ToString());

                if (email.Length == 0 || password.Length == 0 || email.Length > 150 || password.Length > 72)
                    return Results.Redirect(LoginUrl("campos", email, returnUrl, unidade));

                await using var db = await dbFactory.CreateDbContextAsync();
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Ativa);

                var ip = http.Connection.RemoteIpAddress?.ToString();

                if (user is null)
                {
                    BCrypt.Net.BCrypt.Verify(password, DummyHash);
                    await audit.RegistrarAsync(AcaoAuditoria.FalhaAcesso, CategoriaAuditoria.Acesso, "Sessao",
                        new AuditActor(null, SafeEmail(email), "", null, ip), detalhes: "Credenciais inválidas (conta não encontrada ou inativa)");
                    return Results.Redirect(LoginUrl("credenciais", email, returnUrl, unidade));
                }

                if (!user.VerifyPassword(password))
                {
                    await audit.RegistrarAsync(AcaoAuditoria.FalhaAcesso, CategoriaAuditoria.Acesso, "Sessao",
                        new AuditActor(user.Id, user.Name, UiFormat.RoleLabel(user.Role), null, ip), user.Id.ToString(), user.Name, "Senha incorreta");
                    return Results.Redirect(LoginUrl("credenciais", email, returnUrl, unidade));
                }

                // NOVO: grava no cookie a instituição padrão da pessoa (a mais antiga em que ela atua). Sem isso,
                // com 2+ instituições (Faculdade e CLF) o painel ficaria sem instituição selecionada.
                var defaultInstitution = await DefaultInstitutionAsync(db, user);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                };
                if (defaultInstitution is Guid dflt) claims.Add(new Claim(AuditActorAccessor.InstitutionClaim, dflt.ToString()));
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await audit.RegistrarAsync(AcaoAuditoria.Acesso, CategoriaAuditoria.Acesso, "Sessao",
                    new AuditActor(user.Id, user.Name, UiFormat.RoleLabel(user.Role), defaultInstitution, ip), user.Id.ToString(), user.Name, "Login no painel");

                await http.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties
                    {
                        IsPersistent = remember,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(remember ? 24 * 14 : 8)
                    });

                var target = IsLocalUrl(returnUrl) && returnUrl != "/" ? returnUrl : UiFormat.HomePath(user.Role);
                return Results.Redirect(target);
            })
            .DisableAntiforgery()
            .RequireRateLimiting("public-auth");

            // NOVO: troca a instituição em uso (Faculdade, Colégio...). Regrava o cookie com o claim da instituição
            // escolhida; só aceita instituições em que a pessoa tem vínculo ativo (o Admin pode qualquer ativa).
            app.MapPost("/ui/instituicao/selecionar", async (HttpContext http, IDbContextFactory<AppDbContext> dbFactory, AuditoriaService audit) =>
            {
                var auth = await http.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (!auth.Succeeded || auth.Principal is null)
                    return Results.Redirect("/login");

                var form = await http.Request.ReadFormAsync();
                var returnUrl = form["returnUrl"].ToString();
                var principal = auth.Principal;
                var role = Enum.TryParse<UserRole>(principal.FindFirst(ClaimTypes.Role)?.Value, out var r) ? r : UserRole.User;
                var fallback = IsLocalUrl(returnUrl) ? returnUrl : UiFormat.HomePath(role);

                if (!Guid.TryParse(form["id"].ToString(), out var institutionId)
                    || !Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                    return Results.Redirect(fallback);

                await using var db = await dbFactory.CreateDbContextAsync();
                var institution = await db.Instituicoes.AsNoTracking().Where(i => i.Id == institutionId && i.Ativa).Select(i => new { i.Nome }).FirstOrDefaultAsync();
                var allowed = institution is not null && (role == UserRole.Admin
                    || await db.UsuariosInstituicoes.AnyAsync(ui => ui.UsuarioId == userId && ui.InstituicaoId == institutionId && ui.Ativa));
                if (!allowed)
                    return Results.Redirect(fallback);

                var identity = new ClaimsIdentity(
                    principal.Claims.Where(c => c.Type != AuditActorAccessor.InstitutionClaim)
                        .Append(new Claim(AuditActorAccessor.InstitutionClaim, institutionId.ToString())),
                    CookieAuthenticationDefaults.AuthenticationScheme);
                await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), auth.Properties);

                await audit.RegistrarAsync(AcaoAuditoria.Acesso, CategoriaAuditoria.Acesso, "Instituicao",
                    new AuditActor(userId, principal.FindFirst(ClaimTypes.Name)?.Value ?? "", UiFormat.RoleLabel(role), institutionId, http.Connection.RemoteIpAddress?.ToString()),
                    institutionId.ToString(), institution!.Nome, "Troca de instituição");
                return Results.Redirect(fallback);
            })
            .DisableAntiforgery();

            app.MapPost("/ui/logout", async (HttpContext http, AuditoriaService audit) =>
            {
                if (http.User.Identity?.IsAuthenticated == true)
                {
                    Guid? uid = Guid.TryParse(http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g) ? g : null;
                    Guid? inst = Guid.TryParse(http.User.FindFirst(AuditActorAccessor.InstitutionClaim)?.Value, out var i) ? i : null;
                    await audit.RegistrarAsync(AcaoAuditoria.Saida, CategoriaAuditoria.Acesso, "Sessao",
                        new AuditActor(uid, http.User.FindFirst(ClaimTypes.Name)?.Value ?? "", http.User.FindFirst(ClaimTypes.Role)?.Value ?? "", inst,
                            http.Connection.RemoteIpAddress?.ToString()), uid?.ToString(), detalhes: "Logout");
                }
                await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/login");
            })
            .DisableAntiforgery();
        }

        /// <summary>Instituição padrão: a mais antiga em que a pessoa atua (Admin: a mais antiga ativa).</summary>
        private static async Task<Guid?> DefaultInstitutionAsync(AppDbContext db, UserModel user)
        {
            if (user.Role == UserRole.Admin)
                return await db.Instituicoes.AsNoTracking().Where(i => i.Ativa).OrderBy(i => i.CriadaEm).Select(i => (Guid?)i.Id).FirstOrDefaultAsync();
            return await db.UsuariosInstituicoes.AsNoTracking()
                .Where(ui => ui.UsuarioId == user.Id && ui.Ativa && ui.Instituicao!.Ativa)
                .OrderBy(ui => ui.Instituicao!.CriadaEm).Select(ui => (Guid?)ui.InstituicaoId).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Só guarda o texto digitado se parecer um e-mail: quem digita a senha no campo errado não pode ter
        /// essa senha gravada num histórico que não se apaga.
        /// </summary>
        private static string SafeEmail(string email) =>
            email.Length <= 150 && email.Contains('@') && !email.Any(char.IsWhiteSpace) ? email : "(e-mail inválido)";

        private static string LoginUrl(string error, string email, string returnUrl, string unidade = "principal")
        {
            var url = $"/login?erro={Uri.EscapeDataString(error)}&email={Uri.EscapeDataString(email)}";
            if (IsLocalUrl(returnUrl))
                url += $"&ReturnUrl={Uri.EscapeDataString(returnUrl)}";
            url += $"&unidade={Uri.EscapeDataString(unidade)}";
            return url;
        }

        /// <summary>Aceita apenas caminhos internos (evita redirecionamento aberto).</summary>
        public static bool IsLocalUrl(string? url) =>
            !string.IsNullOrEmpty(url)
            && url[0] == '/'
            && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
    }
}
