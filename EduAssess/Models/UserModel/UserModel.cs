namespace EduAssess.Models.UserModel
{
    public class UserModel
    {
        public UserModel(string email, string password)
        {
            Id = Guid.NewGuid();    
            Email = email;
            Password = password;
            Ativa = true;
        }

        public Guid Id { get; init; }
        public string Email { get; private set; }
        public string Password { get; private set; }
        public bool Ativa { get; private set; }

        public void Update(string email, string password)
        {
            Email = email;
            Password = password;
        }

        public void Disable()
        {
            Ativa = false;
        }
    }
}
