using MangaVault.Core.Entities;
using MangaVault.Core.Exceptions;
using MangaVault.Core.Interfaces;

namespace MangaVault.Infrastructure.Persistence.Json;

public class JsonUserRepository(string? storageDirectory = null) : IUserRepository
{
    private readonly JsonFileStore<User> _store = new("users.json", storageDirectory);

    public List<User> GetAll() => _store.ReadAll();

    public User? GetById(int id) =>
        _store.Read(users => users.FirstOrDefault(u => u.UserId == id));

    public User? GetByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        string trimmed = name.Trim();
        return _store.Read(users =>
            users.FirstOrDefault(u => string.Equals(u.Name, trimmed, StringComparison.OrdinalIgnoreCase)));
    }

    public void Save(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        _store.Mutate(users =>
        {
            if (user.UserId <= 0)
            {
                user.AssignId(users.Count == 0 ? 1 : users.Max(u => u.UserId) + 1);
            }

            if (users.Any(u => u.UserId == user.UserId))
                throw new DuplicateUserException(user.UserId);

            if (users.Any(u => string.Equals(u.Name, user.Name, StringComparison.OrdinalIgnoreCase)))
                throw new DuplicateUserException(user.Name);

            users.Add(user);
        });
    }

    public void Update(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        _store.Mutate(users =>
        {
            int index = users.FindIndex(u => u.UserId == user.UserId);
            if (index == -1)
                throw new UserNotFoundException(user.UserId);

            if (users.Any(u => u.UserId != user.UserId && string.Equals(u.Name, user.Name, StringComparison.OrdinalIgnoreCase)))
                throw new DuplicateUserException(user.Name);

            users[index] = user;
        });
    }
}
