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
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin", "Gestor"));
    options.AddPolicy("Professor", policy => policy.RequireRole("Professor"));
});

builder.Services.AddControllers();

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
    // Usa SQLite por padrão; se desejar outro provedor altere a connection string e o Use... correspondente
    var conn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=eduassess.db";
    options.UseSqlite(conn);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Define o Swagger UI como raiz da aplicação para abrir direto ao iniciar.
        c.RoutePrefix = string.Empty; 
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    });
}

app.AuthRoutes();
app.CursosRoutes();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

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
            cpf: "00000000000",
            tel: "0000000000",
            dataNascimento: DateOnly.FromDateTime(new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            password: "Admin1234", // Troque por uma senha forte
            role: UserRole.Admin
        );

        context.Users.Add(adminUser);
        context.SaveChanges();
    }
}

app.Run();
