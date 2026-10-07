using System.Data;

namespace EduAssess.Models.UserModel
{
    public enum UserRole
    {
        Admin = 0,
        Professor = 1,
        Gestor = 2,
        Aluno = 3,
        User = 4
    }
    public class UserModel
    {

        protected UserModel() { }

        public UserModel(string email, string password, UserRole role = UserRole.User)
        {
            Id = Guid.NewGuid();
            Email = email;
            Role = role;
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            
            Ativa = true;
        }

        public Guid Id { get; init; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public UserRole Role { get; private set; }
        public bool Ativa { get; private set; }

        public void Update(string email, UserRole role, string? newPassword = null)
        {
            Email = email;
            Role = role;

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            }
        }

        public bool VerifyPassword(string password)
        {
            return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
        }

        public void Disable()
        {
            Ativa = false;
        }
    }
}
