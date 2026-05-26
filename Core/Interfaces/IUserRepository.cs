using MangaVault.Core.Entities;

namespace MangaVault.Core.Interfaces
{
    public interface IUserRepository
    {
        List<User> GetAll();
        User? GetById (int id);
        User? GetByName(string name);
        void Update (User user);
        void Save(User user);
    }
}