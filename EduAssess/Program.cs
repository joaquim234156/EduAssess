using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using EduAssess.Application.Interfaces;
using EduAssess.Application.Routes;
using EduAssess.Application.Routes.InstituicaoRoutes;
using EduAssess.Application.Routes.UnidadeRoutes;
using EduAssess.Application.Routes.UsuarioInstituicaoRoutes;
using EduAssess.Application.Services;
using EduAssess.Components;
using EduAssess.Converters;
using EduAssess.Data;
using EduAssess.Models.Local;
using EduAssess.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ───────────── Segredo JWT (nunca versionado) ─────────────
var bootLogger = LoggerFactory.Create(l => l.AddConsole()).CreateLogger("Startup");
var jwtSecret = JwtSecretResolver.Resolve(builder.Configuration, builder.Environment, bootLogger);
builder.Configuration["JwtSettings:Secret"] = jwtSecret;

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new DateOnlyPtBrJsonConverter());
});

// ───────────── Autenticação: cookie (painel web) + JWT (API) ─────────────
const string SmartScheme = "CookieOuJwt";

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = SmartScheme;
    options.DefaultAuthenticateScheme = SmartScheme;
    options.DefaultChallengeScheme = SmartScheme;
})
.AddPolicyScheme(SmartScheme, "Cookie ou JWT", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var header = context.Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? JwtBearerDefaults.AuthenticationScheme
            : CookieAuthenticationDefaults.AuthenticationScheme;
    };
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = "eduassess.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/acesso-negado";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events = new CookieAuthenticationEvents
    {
        // Chamadas de API recebem 401/403 em vez de redirecionamento para a página de login.
        OnRedirectToLogin = context =>
        {
            if (ApiPaths.IsApi(context.Request.Path)) context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            else context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            if (ApiPaths.IsApi(context.Request.Path)) context.Response.StatusCode = StatusCodes.Status403Forbidden;
            else context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        },
        // A cada requisição confirma que a conta segue ativa e sincroniza nome, e-mail e perfil com o banco.
        OnValidatePrincipal = async context =>
        {
            var idClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(idClaim, out var userId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var factory = context.HttpContext.RequestServices.GetRequiredService<IDbContextFactory<AppDbContext>>();
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("CookieValidation");
            try
            {
                await using var db = await factory.CreateDbContextAsync();
                var row = await db.Users.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.Name, u.Email, u.Role, u.Ativa })
                    .FirstOrDefaultAsync();

                if (row is null || !row.Ativa)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var current = context.Principal!;
                if (current.FindFirst(ClaimTypes.Role)?.Value != row.Role.ToString()
                    || current.FindFirst(ClaimTypes.Name)?.Value != row.Name
                    || current.FindFirst(ClaimTypes.Email)?.Value != row.Email)
                {
                    var identity = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Name, row.Name),
                        new Claim(ClaimTypes.Email, row.Email),
                        new Claim(ClaimTypes.Role, row.Role.ToString())
                    }, CookieAuthenticationDefaults.AuthenticationScheme);
                    context.ReplacePrincipal(new ClaimsPrincipal(identity));
                    context.ShouldRenew = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Falha ao validar o cookie de autenticação para o usuário {UserId}.", userId);
                throw;
            }
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin", "Gestor"));
    options.AddPolicy("Professor", policy => policy.RequireRole("Professor"));
    options.AddPolicy("Gestor", policy => policy.RequireRole("Gestor"));
    options.AddPolicy("AtribuirAluno", policy => policy.RequireRole("Admin", "Gestor", "Professor"));
    
});

// ───────────── Banco de dados ─────────────
// A fábrica cria contextos curtos (um por operação) para o painel Blazor; a API recebe um contexto por requisição.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=eduassess.db";
builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

// ───────────── Aplicação ─────────────
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped<UiSession>();
builder.Services.AddScoped<AcademicService>();
builder.Services.AddScoped<AccountRegistrationService>();
builder.Services.AddScoped<AccountProfileService>();
builder.Services.AddScoped<ContactService>();
builder.Services.AddScoped<INotaService, NotaService>();
builder.Services.AddScoped<ICursoService, CursoService>();
builder.Services.AddScoped<IDisciplinaService, DisciplinaService>();
builder.Services.AddScoped<IMatriculaService, MatriculaService>();
builder.Services.AddScoped<IUnidadeService, UnidadeService>();
builder.Services.AddScoped<IInstituicaoService, InstituicaoService>();
builder.Services.AddScoped<IUsuarioInstituicaoService, UsuarioInstituicaoService>();
builder.Services.AddScoped<ITurmaService, TurmaService>();

// ALTERADO (questões com IA): o IaQuestaoService nunca tinha sido registrado, então qualquer
// rota que pedisse IIaQuestaoService falharia. AddHttpClient entrega um HttpClient já gerenciado
// para ele; o timeout evita travar o painel se o Gemini demorar demais.
builder.Services.AddHttpClient<IIaQuestaoService, IaQuestaoService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

// NOVO: serviços usados pelas telas Blazor (banco de questões do professor e prática do aluno)
// e armazenamento das fotos de perfil (com compressão).
builder.Services.AddScoped<QuestaoWebService>();
builder.Services.AddScoped<AvaliacaoWebService>();
builder.Services.AddScoped<InstitutionWebService>();   // NOVO: tela "Instituição e unidades" (reaproveita as regras da API)   // provas e simulados (cartões de avaliações)
builder.Services.AddSingleton<ImageStorageService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT abaixo (obtido em POST /auth/login):"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var key = builder.Configuration.GetValue<string>("JwtSettings:Secret");

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

var appLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
appLifetime.ApplicationStopping.Register(() => app.Logger.LogWarning("O host está iniciando o encerramento."));
AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
    app.Logger.LogCritical(eventArgs.ExceptionObject as Exception,
        "Exceção não tratada no processo. IsTerminating={IsTerminating}.", eventArgs.IsTerminating);
AppDomain.CurrentDomain.ProcessExit += (_, _) =>
    app.Logger.LogWarning("O processo EduAssess está saindo.");


app.UseHttpsRedirection();
app.UseStaticFiles();

// Pega o modo de execução (App, ApiOnly, Full)
var appMode = builder.Configuration.GetValue<string>("APP_MODE") ?? "Full";

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("pt-BR")
    .AddSupportedCultures("pt-BR")
    .AddSupportedUICultures("pt-BR"));

app.UseStaticFiles();

if (appMode != "App")
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    });
}

// Páginas 404 do painel (a API continua respondendo com status puro).
app.UseWhen(context => !ApiPaths.IsApi(context.Request.Path) && appMode != "ApiOnly",
    branch => branch.UseStatusCodePagesWithReExecute("/nao-encontrado"));

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();

app.AuthRoutes();
app.CursosRoutes();
app.MatriculaRoutes();
app.TurmaRoutes();
app.DisciplinaRoutes();
app.NotaRoutes();
app.QuestaoRoutes();   // ALTERADO: faltava esta linha, por isso as rotas /questoes/* nem existiam.

// NOVO (Global): Instituição, Unidades (prédios) e vínculo usuário x instituição/unidade.
// Rotas criadas pela equipe e integradas aqui; veja os comentários em cada arquivo de rota.
app.InstituicaoRoutes();
app.UnidadeRoutes();
app.UsuarioInstituicaoRoutes();

if (appMode != "ApiOnly")
{
    app.UiAuthRoutes();

    // NOVO: devolve a foto de perfil. Exige login (as fotos não ficam em pasta pública).
    // A URL sempre leva ?v=<versão>, então o navegador pode guardar a imagem "para sempre"
    // e só baixa de novo quando a pessoa troca de foto.
    app.MapGet("/ui/avatar/{id:guid}", (Guid id, string? t, ImageStorageService images, HttpContext http) =>
    {
        var path = string.Equals(t, "full", StringComparison.OrdinalIgnoreCase)
            ? images.FullPath(id)
            : images.ThumbPath(id);

        if (!File.Exists(path))
            return Results.NotFound();

        http.Response.Headers.CacheControl = "private, max-age=31536000, immutable";
        return Results.File(path, "image/webp");
    })
    .RequireAuthorization();

    // ALTERADO: antes havia aqui um MapGet("/") que redirecionava para /login. Foi retirado:
    // agora a própria página de login atende "/" (veja @page "/" em Login.razor). Assim abrir o
    // site já mostra o login direto, sem depender de redirecionamento nem de cache do navegador.

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();
}

await DatabaseInitializer.InitializeAsync(app.Services);

app.Run();

/// <summary>Prefixos das rotas da API (respondem 401/403/404 em vez de páginas HTML).</summary>
static class ApiPaths
{
    private static readonly string[] Prefixes =
        ["/auth", "/cursos", "/turmas", "/matricula", "/disciplina", "/notas", "/questoes", "/instituicoes", "/unidades", "/usuario-instituicao", "/swagger"];

    // Só conta como API quando há um subcaminho (ex.: /cursos/list). Os caminhos exatos /cursos, /turmas e /notas
    // são páginas do painel.
    public static bool IsApi(PathString path) =>
        Prefixes.Any(prefix => (path.Value ?? "").StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase));
}
