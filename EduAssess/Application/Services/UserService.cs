using EduAssess.Application.Interfaces;
using EduAssess.Data;

namespace EduAssess.Application.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }
    }
}
