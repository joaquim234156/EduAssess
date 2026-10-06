using EduAssess.Models.UserModel;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<UserModel> Users { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=EduAssess.sqlite");
            base.OnConfiguring(optionsBuilder);
        }
    }
}
