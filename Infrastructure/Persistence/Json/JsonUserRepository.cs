using System.Text.Json;
using MangaVault.Core.Entities;
using MangaVault.Core.Interfaces;

namespace MangaVault.Infrastructure.Persistence.Json
{
    public class JsonUserRepository : IUserRepository
    {
        private readonly string _path = Path.Combine("Storage", "users.json");
        public List<User> GetAll()
        {
            if (!File.Exists(_path)) return new List<User>();
            string json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
        }
        public User? GetById(int id)
        {
            return GetAll().FirstOrDefault(m => m.UserId == id);
        }

        public User? GetByName(string name)
        {
            return GetAll().FirstOrDefault(u => u.Name.ToLower() == name.ToLower());
        }

       public void Save(User user)
        {
            var users = GetAll();
           if (users.Any(u => u.UserId == user.UserId || u.Name.ToLower() == user.Name.ToLower()))
            {
                return;
            }
            users.Add(user);
            SaveAll(users);
        }


        private void SaveAll(List<User> users)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var opciones = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(users, opciones);
            File.WriteAllText(_path, json);
        }
    }
}