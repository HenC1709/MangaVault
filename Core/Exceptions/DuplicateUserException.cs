namespace MangaVault.Core.Exceptions;

public class DuplicateUserException : Exception
{
    public int? UserId { get; }
    public string? Name { get; }

    public DuplicateUserException(int userId)
        : base($"Ya existe un usuario con el Id {userId}.")
    {
        UserId = userId;
    }

    public DuplicateUserException(string name)
        : base($"Ya existe un usuario con el nombre '{name}'.")
    {
        Name = name;
    }
}
