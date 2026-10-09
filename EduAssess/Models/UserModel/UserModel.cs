using System.Data;
using System.Globalization;
using System.Text.Json.Serialization;

namespace EduAssess.Models
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
        protected UserModel()
        {
            Matriculas = new List<MatriculaModel>();
        }

        public UserModel(string name, string email, string cpf, string tel, DateTime dataNascimento, string password, UserRole role = UserRole.User)
        {
            Id = Guid.NewGuid();
            Name = name;
            Email = email;
            Cpf = cpf;
            Telefone = tel;
            DataNascimento = dataNascimento;
            Role = role;
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            Ativa = true;

            Matriculas = new List<MatriculaModel>();
        }

        public Guid Id { get; init; }
        public string Name { get; set; }
        public string Email { get; private set; }
        public string Cpf { get; set; }
        public string Telefone { get; set; }
        public DateTime DataNascimento { get; set; }
        public string PasswordHash { get; private set; }
        public UserRole Role { get; private set; }
        public bool Ativa { get; private set; }

        public ICollection<MatriculaModel> Matriculas { get; private set; }

        public void Update(string name, string email)
        {
            Name = name;
            Email = email;
        }

        public void UpdateAdmin(string name, string email, UserRole role)
        {
            Name = name;
            Email = email;
            Role = role;
        }

        public void UpdatePassword(string newPassword, string? confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                throw new ArgumentException("A nova senha e a confirmação não podem estar vazias.");
            }

            if (newPassword != confirmPassword)
            {
                throw new ArgumentException("A nova senha e a confirmação de senha não coincidem.");
            }

            PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
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
