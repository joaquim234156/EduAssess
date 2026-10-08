using EduAssess.Models.UserModel;
using EduAssess.Models.CursoModel;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<UserModel> Users { get; set; }
        public DbSet<CursoModel> Cursos { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=EduAssess.sqlite");
            base.OnConfiguring(optionsBuilder);
        }
    }
}
