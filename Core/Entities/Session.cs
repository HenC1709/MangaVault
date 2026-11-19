using MangaVault.Core.Enums;

namespace MangaVault.Core.Entities;

public sealed class Session
{
    public int UserId { get; }
    public string UserName { get; }
    public Role Role { get; }
    public DateTime StartedAtUtc { get; }

    public Session(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(user.UserId);

        UserId = user.UserId;
        UserName = user.Name;
        Role = user.Role;
        StartedAtUtc = DateTime.UtcNow;
    }
}
