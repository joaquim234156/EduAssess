using EduAssess.Converters;
using EduAssess.Data;
using EduAssess.Application.Routes;
using EduAssess.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var key = builder.Configuration.GetValue<string>("JwtSettings:Secret");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new DateTimePtBrJsonConverter());
});

// Configuração do Scheme de Autenticação JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key!)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization(options =>
{
    // O nome da política agora é "Admin"
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

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
builder.Services.AddScoped<ITurmaService, TurmaService>();

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
        Description = "Insira o token JWT abaixo:"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>();

var app = builder.Build();

// APP_MODE: Full (padrão: painel + API + Swagger), App (painel + API, sem Swagger), ApiOnly (somente API + Swagger).
var appMode = builder.Configuration.GetValue<string>("APP_MODE") ?? "Full";

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.AuthRoutes();
app.MapCursosRoutes();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Garante que o banco de dados e as migrations foram aplicados
    context.Database.Migrate();

    // Se não existir nenhum usuário registrado, cria o Admin padrão
    if (!context.Users.Any())
    {
        var adminUser = new UserModel(
            name: "Admin",
            email: "admin@eduassess.com",
            cpf: "000.000.000-00",
            tel: "(00) 0000-0000",
            dataNascimento: DateOnly.FromDateTime(new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            password: "Admin1234", // Troque por uma senha forte
            role: UserRole.Admin
        );

        context.Users.Add(adminUser);
        context.SaveChanges();
    }
}

app.Run();

/// <summary>Prefixos das rotas da API (respondem 401/403/404 em vez de páginas HTML).</summary>
static class ApiPaths
{
    private static readonly string[] Prefixes =
        ["/auth", "/cursos", "/turmas", "/matricula", "/disciplina", "/notas", "/swagger"];

    // Só conta como API quando há um subcaminho (ex.: /cursos/list). Os caminhos exatos /cursos, /turmas e /notas
    // são páginas do painel.
    public static bool IsApi(PathString path) =>
        Prefixes.Any(prefix => (path.Value ?? "").StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase));
}