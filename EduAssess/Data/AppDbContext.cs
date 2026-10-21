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
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<UserModel> Users { get; set; } = null!;
        public DbSet<CursoModel> Cursos { get; set; } = null!;
        public DbSet<TurmaModel> Turmas { get; set; } = null!;
        public DbSet<MatriculaModel> Matriculas { get; set; } = null!;
        public DbSet<DisciplinaModel> Disciplinas { get; set; } = null!;
        public DbSet<NotaModel> Notas { get; set; } = null!;
        public DbSet<QuestaoModel> Questoes {get; set;} = null!;

        public DbSet<InstituicaoModel> Instituicoes { get; set; } = null!;
        public DbSet<UnidadeModel> Unidades { get; set; } = null!;
        public DbSet<UsuarioInstituicaoModel> UsuariosInstituicoes { get; set; } = null!;



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<UsuarioInstituicaoModel>(e =>
            {
                // chave composta: o mesmo usuário não vincula 2x à mesma instituição
                e.HasKey(x => new { x.UsuarioId, x.InstituicaoId });
                e.HasOne(x => x.Usuario).WithMany(u => u.Instituicoes).HasForeignKey(x => x.UsuarioId);
                e.HasOne(x => x.Instituicao).WithMany().HasForeignKey(x => x.InstituicaoId);
                // guarda "Professor" no banco em vez de 2; se você reordenar o enum, os dados não se corrompem
                e.Property(x => x.Papel).HasConversion<string>();
            });

            modelBuilder.Entity<InstituicaoModel>(e =>
            {
                e.HasIndex(x => x.Slug).IsUnique();
                e.HasIndex(x => x.Cnpj).IsUnique(); // no SQLite, vários NULL são permitidos
            });

            modelBuilder.Entity<UnidadeModel>()
                .HasIndex(x => new { x.InstituicaoId, x.Codigo }).IsUnique();

            var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
                d => d.ToDateTime(TimeOnly.MinValue),
                d => DateOnly.FromDateTime(d));

            var nullableDateOnlyConverter = new ValueConverter<DateOnly?, DateTime?>(
                d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
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
