using System.Data;
using System.Text;
using EduAssess.Models;
using EduAssess.Models.Global;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Data;

/// <summary>
/// Prepara o banco na inicialização:
///  1. Banco novo (vazio): cria o schema direto do modelo e marca todas as migrations como aplicadas.
///  2. Banco existente: aplica as migrations pendentes (inclui o alinhamento de Disciplinas/Contatos).
///  3. Cria o administrador inicial (BootstrapAdmin) e, em Desenvolvimento, dados de demonstração.
/// </summary>
public static class DatabaseInitializer
{
    public const string DevAdminEmail = "admin@eduassess.local";
    public const string DevAdminPassword = "Admin@12345!";
    public const string DemoPassword = "Demo@12345";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        PrepareSchema(db, logger);
        await EnsureAdminAsync(db, config, env, logger);

        if (env.IsDevelopment() && config.GetValue("Seed:DemoData", true))
            await SeedDemoAsync(db, logger);
    }

    private static void PrepareSchema(AppDbContext db, ILogger logger)
    {
        var hasHistory = db.Database.GetAppliedMigrations().Any();
        if (!hasHistory && !HasAnyTable(db))
        {
            logger.LogInformation("Banco novo: criando o schema a partir do modelo atual.");
            db.Database.EnsureCreated();
            db.Database.ExecuteSqlRaw(
                "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (\"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL)");
            foreach (var migrationId in db.Database.GetMigrations())
            {
                db.Database.ExecuteSqlRaw(
                    "INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})",
                    migrationId, "8.0.31");
            }
            return;
        }

        db.Database.Migrate();
    }

    private static bool HasAnyTable(AppDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed) connection.Open();
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }
        finally
        {
            if (wasClosed) connection.Close();
        }
    }

    private static async Task EnsureAdminAsync(AppDbContext db, IConfiguration config, IHostEnvironment env, ILogger logger)
    {
        if (await db.Users.AnyAsync())
            return;

        var email = config["BootstrapAdmin:Email"];
        var password = config["BootstrapAdmin:Password"];
        var configured = !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password);

        if (!configured)
        {
            if (!env.IsDevelopment())
            {
                logger.LogWarning("Nenhum usuário existe e BootstrapAdmin:Email/Password não foram configurados: nenhum administrador foi criado.");
                return;
            }

            email = DevAdminEmail;
            password = DevAdminPassword;
            logger.LogWarning("Administrador de DESENVOLVIMENTO criado: {Email} / {Password} (não use em produção).", email, password);
        }
        else if (password!.Length < 12 || Encoding.UTF8.GetByteCount(password) > 72)
        {
            throw new InvalidOperationException("BootstrapAdmin:Password deve ter pelo menos 12 caracteres e no máximo 72 bytes.");
        }

        db.Users.Add(new UserModel(
            name: config["BootstrapAdmin:Name"] ?? "Administrador",
            email: email!,
            cpf: "00000000000",
            tel: "0000000000",
            dataNascimento: new DateOnly(1990, 1, 1),
            password: password!,
            role: UserRole.Admin));
        await db.SaveChangesAsync();
    }

    private static async Task SeedDemoAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Cursos.AnyAsync() || await db.Users.CountAsync() > 1)
            return;

        UserModel User(string name, string email, string cpf, string tel, UserRole role) =>
            new(name, email, cpf, tel, new DateOnly(1995, 5, 20), DemoPassword, role);

        var gestor = User("Marina Albuquerque", "gestor@eduassess.demo", "11111111111", "11987654001", UserRole.Gestor);
        var prof1 = User("Paulo Ribeiro", "professor@eduassess.demo", "22222222222", "11987654002", UserRole.Professor);
        var prof2 = User("Helena Duarte", "helena@eduassess.demo", "22222222233", "11987654003", UserRole.Professor);
        var alunos = new[]
        {
            User("Lucas Ferreira", "aluno@eduassess.demo", "33333333301", "11987654010", UserRole.Aluno),
            User("Beatriz Nogueira", "beatriz@eduassess.demo", "33333333302", "11987654011", UserRole.Aluno),
            User("Rafael Teixeira", "rafael@eduassess.demo", "33333333303", "11987654012", UserRole.Aluno),
            User("Camila Prado", "camila@eduassess.demo", "33333333304", "11987654013", UserRole.Aluno),
        };
        var pendentes = new[]
        {
            User("Diego Martins", "diego@eduassess.demo", "44444444401", "11987654020", UserRole.User),
            User("Isabela Moraes", "isabela@eduassess.demo", "44444444402", "11987654021", UserRole.User),
        };

        db.Users.AddRange(new[] { gestor, prof1, prof2 }.Concat(alunos).Concat(pendentes));

        var adm = new CursoModel("Administração", "Gestão de organizações, pessoas e processos.", 4);
        var dir = new CursoModel("Direito", "Formação jurídica com foco em prática profissional.", 5);
        var enf = new CursoModel("Enfermagem", "Cuidado em saúde com base em evidências.", 4);
        db.Cursos.AddRange(adm, dir, enf);

        var d1 = new DisciplinaModel("Gestão de Pessoas", "Liderança, clima e desenvolvimento de equipes.", prof1.Id);
        var d2 = new DisciplinaModel("Contabilidade Gerencial", "Custos, orçamento e indicadores financeiros.", prof1.Id);
        var d3 = new DisciplinaModel("Direito Constitucional", "Estado, direitos e garantias fundamentais.", prof2.Id);
        var d4 = new DisciplinaModel("Fundamentos de Enfermagem", "Práticas básicas do cuidado.", prof2.Id);
        db.Disciplinas.AddRange(d1, d2, d3, d4);

        var t1 = new TurmaModel("Administração — Noite", prof1.Id, adm.Id);
        var t2 = new TurmaModel("Direito — Manhã", prof2.Id, dir.Id);
        var t3 = new TurmaModel("Enfermagem — Tarde", prof2.Id, enf.Id);
        db.Turmas.AddRange(t1, t2, t3);

        var m = new[]
        {
            new MatriculaModel(alunos[0].Id, t1.Id), new MatriculaModel(alunos[1].Id, t1.Id),
            new MatriculaModel(alunos[2].Id, t2.Id), new MatriculaModel(alunos[3].Id, t3.Id),
        };
        db.Matriculas.AddRange(m);

        decimal[][] values = [[8.5m, 7.0m, 9.0m], [6.5m, 7.5m, 5.5m], [8.0m, 6.0m, 7.0m], [9.5m, 8.5m, 9.0m]];
        var disciplinasPorMatricula = new[] { (d1, d2), (d1, d2), (d3, d3), (d4, d4) };
        var professores = new[] { prof1, prof1, prof2, prof2 };
        var avaliacoes = new[] { "Prova 1", "Trabalho em grupo", "Prova 2" };
        for (var i = 0; i < m.Length; i++)
        {
            for (var j = 0; j < avaliacoes.Length; j++)
            {
                var disciplina = j == 1 ? disciplinasPorMatricula[i].Item2 : disciplinasPorMatricula[i].Item1;
                db.Notas.Add(new NotaModel(m[i].Id, professores[i].Id, disciplina.Id, avaliacoes[j], values[i][j]));
            }
        }

        db.Contatos.Add(new ContatoModel("Carolina Mendes", "carolina@faculdade.edu.br", "11999990000",
            "Faculdade Horizonte", "Coordenação de curso", "Queremos centralizar notas e matrículas dos cursos de Direito."));

        await db.SaveChangesAsync();
        logger.LogInformation("Dados de demonstração criados. Contas: gestor@eduassess.demo, professor@eduassess.demo, aluno@eduassess.demo (senha {Password}).", DemoPassword);
    }
}
