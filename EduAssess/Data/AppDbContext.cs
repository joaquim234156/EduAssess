using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using EduAssess.Models;

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
        public DbSet<ContatoModel> Contatos { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
