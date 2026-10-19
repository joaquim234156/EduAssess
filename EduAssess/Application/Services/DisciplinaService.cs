using EduAssess.Application.Interfaces;
using EduAssess.Data;

namespace EduAssess.Application.Services
{
    public class DisciplinaService : IDisciplinaService
    {
        private readonly AppDbContext _context;

        public DisciplinaService(AppDbContext context)
        {
            _context = context;
        }
    }
}
