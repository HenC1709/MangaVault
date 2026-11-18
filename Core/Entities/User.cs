using System.Text.Json.Serialization;
using MangaVault.Core.Enums;
using MangaVault.Core.Interfaces;

namespace MangaVault.Core.Entities;

public class User
{
    [JsonInclude]
    public int UserId { get; private set; }
    [JsonInclude]
    public string Name { get; private set; } = "";
    [JsonInclude]
    public string PasswordHash { get; private set; } = "";
    [JsonInclude]
    public Role Role { get; private set; } = Role.User;

    public User() { }

    public User(int userId, string name, string passwordHash, Role rol = Role.User)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        if (!Enum.IsDefined(rol))
            throw new ArgumentOutOfRangeException(nameof(rol), "Rol de usuario no válido.");

        UserId = userId;
        Name = name.Trim();
        PasswordHash = passwordHash;
        Role = rol;
    }

    public static User CreateWithPassword(
        string name,
        string plainPassword,
        IPasswordHasher hasher,
        Role role = Role.User,
        int userId = 0)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentException.ThrowIfNullOrWhiteSpace(plainPassword);

        return new User(userId, name, hasher.Hash(plainPassword), role);
    }

    public bool VerifyPassword(string plainPassword, IPasswordHasher hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        return hasher.Verify(plainPassword, PasswordHash);
    }

    public void ChangePassword(string newPlainPassword, IPasswordHasher hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPlainPassword);

        PasswordHash = hasher.Hash(newPlainPassword);
    }

    public void AssignId(int userId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        if (UserId > 0 && UserId != userId)
            throw new InvalidOperationException("El usuario ya tiene un Id asignado.");

        UserId = userId;
    }
}
