using EduAssess.Application.Interfaces;
using EduAssess.Data;

namespace EduAssess.Application.Services
{
    public class MatriculaService : IMatriculaService
    {
        private readonly AppDbContext _context;

        public MatriculaService(AppDbContext context)
        {
            _context = context;
        }
    }
}
