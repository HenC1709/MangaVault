namespace MangaVault.Core.Exceptions;

public class UserNotFoundException : Exception
{
    public int? UserId { get; }
    public string? Name { get; }

    public UserNotFoundException(int userId)
        : base($"No se encontró un usuario con el Id {userId}.")
    {
        UserId = userId;
    }

    public UserNotFoundException(string name)
        : base($"No se encontró un usuario con el nombre '{name}'.")
    {
        Name = name;
    }
}
