using System.Security.Claims;
using EduAssess.Data;
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
            app.MapPost("/ui/login", async (HttpContext http, IDbContextFactory<AppDbContext> dbFactory) =>
            {
                var form = await http.Request.ReadFormAsync();
                var email = form["email"].ToString().Trim().ToLowerInvariant();
                var password = form["password"].ToString();
                var returnUrl = form["returnUrl"].ToString();
                var remember = form["remember"].ToString() == "on";

                if (email.Length == 0 || password.Length == 0 || email.Length > 150 || password.Length > 72)
                    return Results.Redirect(LoginUrl("campos", email, returnUrl));

                await using var db = await dbFactory.CreateDbContextAsync();
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Ativa);

                if (user is null)
                {
                    BCrypt.Net.BCrypt.Verify(password, DummyHash);
                    return Results.Redirect(LoginUrl("credenciais", email, returnUrl));
                }

                if (!user.VerifyPassword(password))
                    return Results.Redirect(LoginUrl("credenciais", email, returnUrl));

                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                }, CookieAuthenticationDefaults.AuthenticationScheme);

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

            app.MapPost("/ui/logout", async (HttpContext http) =>
            {
                await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/");
            })
            .DisableAntiforgery();
        }

        private static string LoginUrl(string error, string email, string returnUrl)
        {
            var url = $"/login?erro={Uri.EscapeDataString(error)}&email={Uri.EscapeDataString(email)}";
            if (IsLocalUrl(returnUrl))
                url += $"&ReturnUrl={Uri.EscapeDataString(returnUrl)}";
            return url;
        }

        /// <summary>Aceita apenas caminhos internos (evita redirecionamento aberto).</summary>
        public static bool IsLocalUrl(string? url) =>
            !string.IsNullOrEmpty(url)
            && url[0] == '/'
            && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
    }
}
