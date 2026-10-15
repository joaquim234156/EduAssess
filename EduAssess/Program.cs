using EduAssess.Converters;
using EduAssess.Application.Routes;
using EduAssess.Models;
using EduAssess.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.Sqlite;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using System.Text;
using System.Security.Cryptography;
using EduAssess.Routes.TurmaRoutes;
using EduAssess.Components;
using EduAssess.Web;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

var key = builder.Configuration["JwtSettings:Secret"];
if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
    throw new InvalidOperationException("Configure JwtSettings:Secret com pelo menos 32 bytes usando um secret manager ou variável de ambiente.");
var keyFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
if (keyFingerprint == "0FF4E66D64D9C80091BCCA39698054BCD5E0F37A27302C5655CED5D3DEC19C16")
    throw new InvalidOperationException("JwtSettings:Secret corresponde a uma chave comprometida do histórico Git. Gere e configure uma chave nova.");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new DateOnlyPtBrJsonConverter());
});

// Configuração do Scheme de Autenticação JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Gestor", policy => policy.RequireRole("Gestor"));
    options.AddPolicy("Gestao", policy => policy.RequireRole("Admin", "Gestor"));
    options.AddPolicy("AtribuirAluno", policy => policy.RequireRole("Admin", "Gestor", "Professor"));
    options.AddPolicy("Professor", policy => policy.RequireRole("Professor"));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("public-auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddControllers();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped<UiSession>();
builder.Services.AddScoped<AcademicDashboardService>();
builder.Services.AddScoped<AccountRegistrationService>();

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

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=eduassess.db";
    var sqliteConnection = new SqliteConnectionStringBuilder(connectionString);
    if (!string.Equals(sqliteConnection.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase)
        && !Path.IsPathRooted(sqliteConnection.DataSource))
    {
        sqliteConnection.DataSource = Path.GetFullPath(sqliteConnection.DataSource, builder.Environment.ContentRootPath);
    }

    options.UseSqlite(sqliteConnection.ToString());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    });
}

app.AuthRoutes();
app.CursosRoutes();
app.MatriculaRoutes();
app.TurmaRoutes();

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (!string.IsNullOrEmpty(path) &&
        (path.Equals("/index.html", StringComparison.OrdinalIgnoreCase) ||
         path.Equals("/index.htm", StringComparison.OrdinalIgnoreCase)))
    {
        context.Response.Redirect("/", false);
        return;
    }

    await next();
});

app.UseStaticFiles();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    context.Database.Migrate();

    // The first administrator must be provisioned through deployment secrets.
    if (!context.Users.Any())
    {
        var adminEmail = app.Configuration["BootstrapAdmin:Email"];
        var adminPassword = app.Configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(adminEmail)
            || string.IsNullOrWhiteSpace(adminPassword)
            || adminEmail.Length > 150
            || !new EmailAddressAttribute().IsValid(adminEmail)
            || adminPassword.Length < 12
            || Encoding.UTF8.GetByteCount(adminPassword) > 72)
        {
            throw new InvalidOperationException("Configure BootstrapAdmin:Email e uma senha entre 12 e 72 bytes para provisionar o primeiro administrador.");
        }

        var adminUser = new UserModel(
            name: "Admin",
            email: adminEmail,
            cpf: "00000000000",
            tel: "0000000000",
            dataNascimento: new DateOnly(1990, 1, 1),
            password: adminPassword,
            role: UserRole.Admin);

        context.Users.Add(adminUser);
        context.SaveChanges();
    }
}

app.Run();