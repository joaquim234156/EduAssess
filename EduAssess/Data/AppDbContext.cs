using EduAssess.Models;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<UserModel> Users { get; set; }
        public DbSet<CursoModel> Cursos { get; set; }
        public DbSet<NotaModel> Notas { get; set; }
        public DbSet<TurmaModel> Turmas { get; set; }
        public DbSet<MatriculaModel> Matriculas { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=EduAssess.sqlite");
            base.OnConfiguring(optionsBuilder);
        }
    }
}
