using EduAssess.Data.Audit;
using EduAssess.Models;
using EduAssess.Models.Global;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq;

namespace EduAssess.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IAuditActorAccessor? _auditActor;

        // NOVO: o accessor é resolvido pela fábrica de contextos (escopo do circuito/requisição). É opcional
        // para continuar funcionando em ferramentas de design e testes.
        public AppDbContext(DbContextOptions<AppDbContext> options, IAuditActorAccessor? auditActor = null) : base(options)
        {
            _auditActor = auditActor;
        }

        /// <summary>NOVO: quem está operando agora (lido na hora de salvar, para refletir a sessão atual).</summary>
        public AuditActor? CurrentActor => _auditActor?.Current;

        /// <summary>NOVO: só o DatabaseInitializer usa (dados iniciais/demonstração não são ação de usuário).</summary>
        public bool SuppressAudit { get; set; }

        public DbSet<UserModel> Users { get; set; } = null!;
        public DbSet<CursoModel> Cursos { get; set; } = null!;
        public DbSet<TurmaModel> Turmas { get; set; } = null!;
        public DbSet<MatriculaModel> Matriculas { get; set; } = null!;
        public DbSet<DisciplinaModel> Disciplinas { get; set; } = null!;
        public DbSet<NotaModel> Notas { get; set; } = null!;
        public DbSet<QuestaoModel> Questoes {get; set;} = null!;
        public DbSet<AvaliacaoModel> Avaliacoes { get; set; } = null!;
        public DbSet<AvaliacaoQuestaoModel> AvaliacoesQuestoes { get; set; } = null!;

        public DbSet<ContatoModel> Contatos { get; set; } = null!;
        public DbSet<InstituicaoModel> Instituicoes { get; set; } = null!;
        public DbSet<MantenedorModel> Mantenedores { get; set; } = null!;
        public DbSet<UnidadeModel> Unidades { get; set; } = null!;
        public DbSet<UsuarioInstituicaoModel> UsuariosInstituicoes { get; set; } = null!;
        public DbSet<SeloTipoModel> SelosTipos { get; set; } = null!;
        public DbSet<QuestaoSeloModel> QuestoesSelos { get; set; } = null!;
        public DbSet<ResultadoPraticaModel> ResultadosPratica { get; set; } = null!;
        public DbSet<RegistroAuditoriaModel> RegistrosAuditoria { get; set; } = null!;



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<UsuarioInstituicaoModel>(e =>
            {
                // chave composta: o mesmo usuário não vincula 2x à mesma instituição
                e.HasKey(x => new { x.UsuarioId, x.InstituicaoId });
                e.HasOne(x => x.Usuario).WithMany(u => u.Instituicoes).HasForeignKey(x => x.UsuarioId);
                e.HasOne(x => x.Instituicao).WithMany().HasForeignKey(x => x.InstituicaoId);
                // NOVO: unidade (prédio) da pessoa. Restrict: não dá para apagar uma unidade que ainda tem gente lotada.
                e.HasOne(x => x.Unidade).WithMany().HasForeignKey(x => x.UnidadeId).OnDelete(DeleteBehavior.Restrict);
                // guarda "Professor" no banco em vez de 2; se você reordenar o enum, os dados não se corrompem
                e.Property(x => x.Papel).HasConversion<string>();
            });

            modelBuilder.Entity<InstituicaoModel>(e =>
            {
                e.HasIndex(x => x.Slug).IsUnique();
                e.HasIndex(x => x.Cnpj).IsUnique(); // no SQLite, vários NULL são permitidos
                // NOVO: Faculdade ou Colégio (CLF), guardado como texto (igual ao Papel)
                e.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20).HasDefaultValue(TipoInstituicao.Faculdade);
                e.Property(x => x.Sigla).HasMaxLength(10);
                // Já existiam como default de banco (migration AddInstitutionTenancyAndMaintainer); declarar aqui mantém o snapshot fiel.
                e.Property(x => x.CorPrimaria).HasDefaultValue("#1F4E79");
                e.Property(x => x.CorSecundaria).HasDefaultValue("#D6A94B");
                e.HasOne(x => x.Mantenedor).WithMany(x => x.Instituicoes)
                    .HasForeignKey(x => x.MantenedorId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CursoModel>().HasOne<InstituicaoModel>()
                .WithMany().HasForeignKey(x => x.InstituicaoId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CursoModel>().HasIndex(x => x.InstituicaoId);
            modelBuilder.Entity<DisciplinaModel>().HasOne<InstituicaoModel>()
                .WithMany().HasForeignKey(x => x.InstituicaoId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DisciplinaModel>().HasIndex(x => x.InstituicaoId);
            modelBuilder.Entity<TurmaModel>().HasOne<InstituicaoModel>()
                .WithMany().HasForeignKey(x => x.InstituicaoId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TurmaModel>().HasIndex(x => x.InstituicaoId);
            modelBuilder.Entity<MatriculaModel>().HasOne<InstituicaoModel>()
                .WithMany().HasForeignKey(x => x.InstituicaoId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MatriculaModel>().HasIndex(x => x.InstituicaoId);
            modelBuilder.Entity<NotaModel>().HasOne<InstituicaoModel>()
                .WithMany().HasForeignKey(x => x.InstituicaoId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<NotaModel>().HasIndex(x => x.InstituicaoId);
            modelBuilder.Entity<QuestaoModel>().HasOne<InstituicaoModel>()
                .WithMany().HasForeignKey(x => x.InstituicaoId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<QuestaoModel>().HasIndex(x => x.InstituicaoId);
            modelBuilder.Entity<AvaliacaoModel>().HasOne<InstituicaoModel>()
                .WithMany().HasForeignKey(x => x.InstituicaoId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AvaliacaoModel>().HasIndex(x => x.InstituicaoId);

            modelBuilder.Entity<UnidadeModel>(e =>
            {
                e.HasIndex(x => new { x.InstituicaoId, x.Sigla, x.Slug }).IsUnique();
                e.Property(x => x.CorPrimaria).HasMaxLength(7);
                e.Property(x => x.CorSecundaria).HasMaxLength(7);
            });

            // NOVO: avaliações (provas/simulados). O código (ex.: PSI-26-10-9GN) não pode se repetir.
            modelBuilder.Entity<AvaliacaoModel>(e =>
            {
                e.HasIndex(x => x.Codigo).IsUnique();
            });

            // NOVO: ligação avaliação x questão. A mesma questão não entra 2x na mesma avaliação;
            // apagar a avaliação apaga só as ligações (as questões continuam no banco).
            modelBuilder.Entity<AvaliacaoQuestaoModel>(e =>
            {
                e.HasKey(x => new { x.AvaliacaoId, x.QuestaoId });
                e.HasOne(x => x.Avaliacao).WithMany(a => a.Questoes).HasForeignKey(x => x.AvaliacaoId).OnDelete(DeleteBehavior.Cascade);
            });

            // NOVO: catálogo de selos e selos aplicados às questões (sem duplicidade: chave questão + tipo).
            modelBuilder.Entity<SeloTipoModel>(e =>
            {
                e.HasIndex(x => x.Codigo).IsUnique();
                e.Property(x => x.Codigo).HasMaxLength(30);
                e.Property(x => x.Nome).HasMaxLength(60);
                e.Property(x => x.Cor).HasMaxLength(20);
                e.Property(x => x.RotuloDetalhe).HasMaxLength(60);
                e.Property(x => x.Descricao).HasMaxLength(200);
            });
            modelBuilder.Entity<QuestaoSeloModel>(e =>
            {
                e.HasKey(x => new { x.QuestaoId, x.SeloTipoId });
                e.HasOne(x => x.Questao).WithMany(q => q.Selos).HasForeignKey(x => x.QuestaoId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.SeloTipo).WithMany().HasForeignKey(x => x.SeloTipoId).OnDelete(DeleteBehavior.Restrict);
                e.Property(x => x.Detalhe).HasMaxLength(80);
                e.HasIndex(x => x.SeloTipoId);
            });

            // NOVO: resultados de treino/simulado do aluno (separados das notas oficiais).
            modelBuilder.Entity<ResultadoPraticaModel>(e =>
            {
                e.HasOne<InstituicaoModel>().WithMany().HasForeignKey(x => x.InstituicaoId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<UserModel>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
                e.Property(x => x.Origem).HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.Titulo).HasMaxLength(120);
                e.Property(x => x.DisciplinaNome).HasMaxLength(100);
                e.Property(x => x.Nota).HasPrecision(4, 2);
                e.HasIndex(x => new { x.UsuarioId, x.InstituicaoId, x.RealizadoEm });
            });

            // NOVO: trilha de auditoria (somente inserção; veja também o gatilho criado na migration).
            modelBuilder.Entity<RegistroAuditoriaModel>(e =>
            {
                e.Property(x => x.Acao).HasConversion<string>().HasMaxLength(30);
                e.Property(x => x.Categoria).HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.Entidade).HasMaxLength(60);
                e.Property(x => x.EntidadeId).HasMaxLength(100);
                e.Property(x => x.EntidadeRotulo).HasMaxLength(200);
                e.Property(x => x.Detalhes).HasMaxLength(800);
                e.Property(x => x.AtorNome).HasMaxLength(150);
                e.Property(x => x.AtorPerfil).HasMaxLength(40);
                e.Property(x => x.Ip).HasMaxLength(64);
                e.HasIndex(x => new { x.InstituicaoId, x.OcorridoEm });
                e.HasIndex(x => new { x.ProfessorResponsavelId, x.OcorridoEm });
                e.HasIndex(x => x.AtorId);
                e.HasIndex(x => new { x.Categoria, x.OcorridoEm });
            });

            var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
                d => DateTime.SpecifyKind(d.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
                d => DateOnly.FromDateTime(d));

            var nullableDateOnlyConverter = new ValueConverter<DateOnly?, DateTime?>(
                d => d.HasValue ? DateTime.SpecifyKind(d.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc) : (DateTime?)null,
                d => d.HasValue ? DateOnly.FromDateTime(d.Value) : (DateOnly?)null);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;
                var properties = clrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(DateOnly) || p.PropertyType == typeof(DateOnly?));

                foreach (var prop in properties)
                {
                    if (prop.PropertyType == typeof(DateOnly))
                        modelBuilder.Entity(clrType).Property(prop.Name).HasConversion(dateOnlyConverter);
                    else
                        modelBuilder.Entity(clrType).Property(prop.Name).HasConversion(nullableDateOnlyConverter);
                }
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}
