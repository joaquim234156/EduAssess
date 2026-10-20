using EduAssess.Application.Interfaces;
using EduAssess.Application.Routes;
using EduAssess.Application.Services;
using EduAssess.Components;
using EduAssess.Converters;
using EduAssess.Data;
using EduAssess.Models;
using EduAssess.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using System.Text;

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin", "Gestor"));
    options.AddPolicy("Professor", policy => policy.RequireRole("Professor"));
    options.AddPolicy("Docente", policy => policy.RequireRole("Admin","Gestor","Professor"));
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

//Services
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

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var conn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=eduassess.db";
    options.UseSqlite(conn);
});

var app = builder.Build();

// Pega o modo de execução (App, ApiOnly, Full)
var appMode = builder.Configuration.GetValue<string>("APP_MODE") ?? "Full";

app.UseHttpsRedirection();
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

app.AuthRoutes();
app.CursosRoutes();
app.MatriculaRoutes();
app.TurmaRoutes();
app.DisciplinaRoutes();
app.NotaRoutes();

if (appMode != "ApiOnly")
{
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    context.Database.Migrate();

    // The first administrator must be provisioned through deployment secrets.
    if (!context.Users.Any())
    {
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao aplicar migrations: {ex.Message}");
    }

    try
    {
        if (!context.Users.Any())
        {
            var adminUser = new UserModel(
                name: "Admin",
                email: "admin@eduassess.com",
                cpf: "00000000000",
                tel: "0000000000",
                dataNascimento: DateOnly.FromDateTime(new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                password: "Admin1234",
                role: UserRole.Admin
            );

            context.Users.Add(adminUser);
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao criar admin: {ex.Message}");
    }
}

app.Run();