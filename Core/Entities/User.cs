using MangaVault.Core.Enums;

namespace MangaVault.Core.Entities
{
    public class User
    {
        public int UserId { get; private set; }
        public string Name {get; private set;} = "";
        public string PasswordHash {get; private set;} = "";
        public Role Role {get; private set;} = Role.User;
        
        public User() {}
        public User(int userId, string name, string passwordHash, Role rol = Role.User)
        {
            UserId = userId;
            Name = name;
            PasswordHash = passwordHash;
            Role = rol;
        }
    }
}