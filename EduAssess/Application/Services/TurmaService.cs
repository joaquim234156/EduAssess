using EduAssess.Application.Interfaces;
using EduAssess.Data;

namespace EduAssess.Application.Services
{
    public class TurmaService : ITurmaService
    {
        private readonly AppDbContext _context;

        public TurmaService(AppDbContext context)
        {
            _context = context;
        }
    }
}
