using System.Data;
using System.Text;
using EduAssess.Models;
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

        // NOVO: a instituição (Faculdade Luciano Feijão) e seus prédios existem em QUALQUER ambiente.
        // São dados de configuração da faculdade, não de demonstração. Desligue com "Seed:BaseInstitution": false.
        await SeedBaseInstitutionAsync(db, config, logger);

        if (env.IsDevelopment() && config.GetValue("Seed:DemoData", true))
        {
            await SeedDemoAsync(db, logger);
            await SeedDemoQuestionsAsync(db, logger);
            await SeedDemoExamsAsync(db, logger);
            await SeedDemoUnitLinksAsync(db, logger);   // NOVO: distribui as contas de demonstração pelos três prédios
        }
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

    // NOVO: questões de exemplo, para o aluno conseguir praticar mesmo sem a chave da IA configurada.
    // Só roda em Desenvolvimento, só se o banco ainda não tem questões e só nas disciplinas demo.
    private static async Task SeedDemoQuestionsAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Questoes.AnyAsync())
            return;

        var gestao = await db.Disciplinas.FirstOrDefaultAsync(d => d.Nome == "Gestão de Pessoas");
        var direito = await db.Disciplinas.FirstOrDefaultAsync(d => d.Nome == "Direito Constitucional");
        if (gestao is null || direito is null)
            return;

        void Add(DisciplinaModel d, string assunto, DificuldadeQuestao dif, string enunciado, string correta,
            string[] erradas, string explicacao)
        {
            // A correta entra na posição 3 só para o exemplo; a prática embaralha as alternativas.
            var a = new List<string>(erradas);
            a.Insert(2, correta);
            db.Questoes.Add(new QuestaoModel(d.ProfessorId, d.Id, enunciado, correta,
                a[0], a[1], a[2], a[3], a[4], assunto, explicacao, dif));
        }

        Add(gestao, "Liderança", DificuldadeQuestao.Facil,
            "Qual característica é mais associada a um líder que desenvolve a própria equipe?",
            "Dar feedback frequente e apoiar o crescimento de cada pessoa",
            ["Centralizar todas as decisões", "Evitar conversas difíceis", "Comparar publicamente os colaboradores", "Manter as metas em segredo"],
            "Líderes que desenvolvem equipes oferecem feedback contínuo e apoiam o crescimento individual.");
        Add(gestao, "Liderança", DificuldadeQuestao.Media,
            "Na liderança situacional, o líder deve:",
            "Adaptar o estilo de liderança ao nível de maturidade de cada liderado",
            ["Usar sempre o mesmo estilo para todos", "Delegar tudo desde o primeiro dia", "Evitar acompanhar resultados", "Priorizar apenas a antiguidade"],
            "O modelo situacional defende que o estilo muda conforme a competência e o comprometimento do liderado.");
        Add(gestao, "Clima organizacional", DificuldadeQuestao.Facil,
            "O que a pesquisa de clima organizacional busca medir?",
            "A percepção dos colaboradores sobre o ambiente de trabalho",
            ["O lucro anual da empresa", "A temperatura das salas", "O número de clientes novos", "O tempo de casa do presidente"],
            "A pesquisa de clima mapeia como as pessoas percebem o ambiente, a gestão e as condições de trabalho.");
        Add(gestao, "Motivação", DificuldadeQuestao.Media,
            "Segundo Herzberg, fatores como reconhecimento e responsabilidade são classificados como:",
            "Fatores motivacionais",
            ["Fatores higiênicos", "Fatores externos de controle", "Fatores de punição", "Fatores de rotatividade"],
            "Na teoria dos dois fatores, reconhecimento e responsabilidade são motivacionais; salário e condições são higiênicos.");
        Add(gestao, "Motivação", DificuldadeQuestao.Dificil,
            "Na Teoria da Expectativa de Vroom, a motivação depende da combinação de:",
            "Expectativa, instrumentalidade e valência",
            ["Salário, cargo e tempo de casa", "Autonomia, poder e status", "Medo, punição e recompensa fixa", "Rotina, controle e hierarquia"],
            "Vroom propõe que a motivação resulta da expectativa de esforço, da instrumentalidade do desempenho e do valor (valência) da recompensa.");

        Add(direito, "Direitos fundamentais", DificuldadeQuestao.Facil,
            "A dignidade da pessoa humana, na Constituição Federal de 1988, é:",
            "Um dos fundamentos da República Federativa do Brasil",
            ["Apenas um objetivo secundário", "Uma garantia só para brasileiros natos", "Uma norma sem valor jurídico", "Um princípio do direito tributário"],
            "O art. 1º, III, da CF/88 coloca a dignidade da pessoa humana entre os fundamentos da República.");
        Add(direito, "Poderes da União", DificuldadeQuestao.Facil,
            "Quais são os Poderes da União, independentes e harmônicos entre si?",
            "Legislativo, Executivo e Judiciário",
            ["Federal, Estadual e Municipal", "Executivo, Militar e Eclesiástico", "Legislativo, Fiscal e Administrativo", "Judiciário, Policial e Tributário"],
            "O art. 2º da CF/88 prevê o Legislativo, o Executivo e o Judiciário como Poderes da União.");
        Add(direito, "Direitos fundamentais", DificuldadeQuestao.Media,
            "O habeas corpus é o remédio constitucional adequado para proteger:",
            "A liberdade de locomoção",
            ["O direito de acesso a informações públicas", "O direito líquido e certo não amparado por outro remédio", "O patrimônio público", "A propriedade intelectual"],
            "O habeas corpus protege a liberdade de ir e vir quando ameaçada ou violada por ilegalidade ou abuso de poder.");
        Add(direito, "Controle de constitucionalidade", DificuldadeQuestao.Dificil,
            "No Brasil, o controle concentrado de constitucionalidade de lei federal é de competência do:",
            "Supremo Tribunal Federal",
            ["Superior Tribunal de Justiça", "Tribunal Superior Eleitoral", "Conselho Nacional de Justiça", "Tribunal de Contas da União"],
            "Cabe ao STF, como guardião da Constituição, processar e julgar ADI e ADC contra leis e atos normativos federais.");

        await db.SaveChangesAsync();
        logger.LogInformation("Questões de demonstração criadas (Gestão de Pessoas e Direito Constitucional).");
    }

    // NOVO: avaliações de exemplo (uma aplicada e duas em rascunho) para a tela /avaliacoes e a
    // prática do aluno terem o que mostrar. Só em Desenvolvimento e só se ainda não há avaliações.
    private static async Task SeedDemoExamsAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Avaliacoes.AnyAsync())
            return;

        var gestao = await db.Disciplinas.FirstOrDefaultAsync(d => d.Nome == "Gestão de Pessoas");
        var direito = await db.Disciplinas.FirstOrDefaultAsync(d => d.Nome == "Direito Constitucional");
        var gestor = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Gestor);
        if (gestao is null || direito is null)
            return;

        async Task Create(DisciplinaModel d, Guid autor, string titulo, TipoAvaliacao tipo, string detalhes,
            PeriodicidadeAvaliacao per, int dur, bool aplicar)
        {
            var exam = new AvaliacaoModel(AvaliacaoModel.GerarCodigo(d.Nome, DateTime.Now), titulo, tipo, autor, d.Id, detalhes, per, dur);
            db.Avaliacoes.Add(exam);

            var ids = await db.Questoes.Where(q => q.Ativa && q.DisciplinaId == d.Id).Select(q => q.Id).ToListAsync();
            var ordem = 1;
            foreach (var qid in ids)
                db.AvaliacoesQuestoes.Add(new AvaliacaoQuestaoModel(exam.Id, qid, ordem++));

            if (aplicar && ids.Count > 0)
                exam.Aplicar();
        }

        await Create(gestao, gestao.ProfessorId, "Gestão de Pessoas", TipoAvaliacao.Prova,
            "Prova bimestral sobre liderança, clima e motivação.", PeriodicidadeAvaliacao.Bimestral, 2, aplicar: true);
        await Create(direito, direito.ProfessorId, "Direito Constitucional", TipoAvaliacao.Simulado,
            "Simulado de revisão para a prova final.", PeriodicidadeAvaliacao.Semestral, 1, aplicar: false);
        if (gestor is not null)
            await Create(gestao, gestor.Id, "Revisão Geral", TipoAvaliacao.Simulado, "", PeriodicidadeAvaliacao.Unica, 0, aplicar: false);

        await db.SaveChangesAsync();
        logger.LogInformation("Avaliações de demonstração criadas.");
    }

    // ───────────────────────── NOVO: instituição, unidades e lotação ─────────────────────────

    /// <summary>
    /// Cria a Faculdade Luciano Feijão e os três prédios (Principal, Anexo e Odontologia) se ainda não
    /// existir nenhuma instituição. Só roda uma vez: depois disso, a tela "Instituição e unidades" (ou a API)
    /// cuida de tudo. CONFERIR: o CNPJ e o endereço vieram do documento de autoria da faculdade.
    /// </summary>
    private static async Task SeedBaseInstitutionAsync(AppDbContext db, IConfiguration config, ILogger logger)
    {
        if (!config.GetValue("Seed:BaseInstitution", true) || await db.Instituicoes.AnyAsync())
            return;

        var institution = new InstituicaoModel(
            "Faculdade Luciano Feijão", "09.533.217/0003-01", "", "",
            "Rua José Lopes Ponte, 400, Dom Expedito, Sobral-CE, CEP 62050-215");
        db.Instituicoes.Add(institution);

        db.Unidades.AddRange(
            new UnidadeModel(institution.Id, "Prédio Principal", "PP", TipoUnidade.Campus),
            new UnidadeModel(institution.Id, "Anexo", "ANX", TipoUnidade.Campus),   // ALTERADO: era "Prédio Medicina"
            new UnidadeModel(institution.Id, "Prédio Odontologia", "ODO", TipoUnidade.Campus));

        await db.SaveChangesAsync();
        logger.LogInformation("Instituição base criada: Faculdade Luciano Feijão (Prédio Principal, Anexo e Odontologia).");
    }

    /// <summary>
    /// Só em Desenvolvimento: espalha as contas de demonstração pelos prédios (Principal, Anexo, Odontologia) e cria
    /// o curso de Medicina com um professor e um aluno, para dar para ver todas as combinações de visual.
    /// O administrador fica SEM unidade ("rede"). O visual de Medicina vem do CURSO, não do prédio.
    /// </summary>
    private static async Task SeedDemoUnitLinksAsync(AppDbContext db, ILogger logger)
    {
        var institution = await db.Instituicoes.OrderBy(i => i.Nome).FirstOrDefaultAsync();
        if (institution is null || await db.UsuariosInstituicoes.AnyAsync(ui => ui.UnidadeId != null))
            return;

        var units = await db.Unidades.Where(u => u.InstituicaoId == institution.Id).ToListAsync();
        Guid? Unit(string slugPart) => units.FirstOrDefault(u => u.Slug.Contains(slugPart))?.Id;
        var principal = Unit("principal"); var anexo = Unit("anexo"); var odonto = Unit("odonto");
        if (principal is null || anexo is null || odonto is null)
            return;

        // Contas extras para completar as combinações. Criadas só se ainda não existirem.
        var extras = new (string Name, string Email, string Cpf, string Tel, UserRole Role)[]
        {
            ("Ricardo Sampaio", "gestor.anexo@eduassess.demo", "11111111112", "11987654101", UserRole.Gestor),
            ("Larissa Cunha", "gestor.odonto@eduassess.demo", "11111111113", "11987654102", UserRole.Gestor),
            ("Dr. André Valença", "professor.medicina@eduassess.demo", "22222222244", "11987654103", UserRole.Professor),
            ("Júlia Matos", "aluno.medicina@eduassess.demo", "33333333305", "11987654104", UserRole.Aluno),
            ("Pedro Lacerda", "aluno.anexo@eduassess.demo", "33333333306", "11987654105", UserRole.Aluno),
        };
        foreach (var x in extras)
        {
            if (!await db.Users.AnyAsync(u => u.Email == x.Email))
                db.Users.Add(new UserModel(x.Name, x.Email, x.Cpf, x.Tel, new DateOnly(1992, 3, 10), DemoPassword, x.Role));
        }
        await db.SaveChangesAsync();

        // Curso de Medicina: o professor e o aluno de Medicina ganham o visual do CURSO (teal), mesmo estando no Anexo.
        var profMed = await db.Users.FirstAsync(u => u.Email == "professor.medicina@eduassess.demo");
        var alunoMed = await db.Users.FirstAsync(u => u.Email == "aluno.medicina@eduassess.demo");
        if (!await db.Cursos.AnyAsync(c => c.Nome == "Medicina"))
        {
            var medicina = new CursoModel("Medicina", "Formação médica com prática clínica desde os primeiros semestres.", 6);
            db.Cursos.Add(medicina);
            var turma = new TurmaModel("Medicina — Integral", profMed.Id, medicina.Id);
            db.Turmas.Add(turma);
            var matricula = new MatriculaModel(alunoMed.Id, turma.Id);
            db.Matriculas.Add(matricula);
            var anatomia = new DisciplinaModel("Anatomia Humana", "Estruturas e sistemas do corpo humano.", profMed.Id);
            db.Disciplinas.Add(anatomia);
            db.Notas.Add(new NotaModel(matricula.Id, profMed.Id, anatomia.Id, "Prova 1", 8.5m));
            db.Notas.Add(new NotaModel(matricula.Id, profMed.Id, anatomia.Id, "Seminário", 9.0m));
            await db.SaveChangesAsync();
        }

        // E-mail -> prédio. O admin@eduassess.local não entra: fica na "rede".
        var plan = new (string Email, Guid? Unit)[]
        {
            ("gestor@eduassess.demo", principal), ("gestor.anexo@eduassess.demo", anexo), ("gestor.odonto@eduassess.demo", odonto),
            ("professor@eduassess.demo", principal), ("helena@eduassess.demo", odonto), ("professor.medicina@eduassess.demo", anexo),
            ("aluno@eduassess.demo", principal), ("camila@eduassess.demo", anexo),
            ("beatriz@eduassess.demo", odonto), ("rafael@eduassess.demo", principal),
            ("aluno.medicina@eduassess.demo", anexo), ("aluno.anexo@eduassess.demo", anexo),
        };

        foreach (var (email, unit) in plan)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null) continue;

            var papel = user.Role switch
            {
                UserRole.Aluno => PapelInstituicao.Aluno,
                UserRole.Professor => PapelInstituicao.Professor,
                _ => PapelInstituicao.Gestor
            };

            var link = await db.UsuariosInstituicoes.FirstOrDefaultAsync(ui => ui.UsuarioId == user.Id && ui.InstituicaoId == institution.Id);
            if (link is null)
                db.UsuariosInstituicoes.Add(new UsuarioInstituicaoModel(user.Id, institution.Id, papel, unit));
            else
                link.Reativar(papel, unit);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Contas de demonstração distribuídas (Principal, Anexo, Odontologia) e curso de Medicina criado.");
    }
}
