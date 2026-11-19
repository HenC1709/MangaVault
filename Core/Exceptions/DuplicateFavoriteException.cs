namespace MangaVault.Core.Exceptions;

public class DuplicateFavoriteException : Exception
{
    public int UserId { get; }
    public int MangaId { get; }

    public DuplicateFavoriteException(int userId, int mangaId)
        : base($"El usuario {userId} ya tiene registrada la obra {mangaId} en su biblioteca.")
    {
        UserId = userId;
        MangaId = mangaId;
    }
}
