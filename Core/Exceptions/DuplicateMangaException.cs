using MangaVault.Core.Enums;

namespace MangaVault.Core.Exceptions;

public class DuplicateMangaException : Exception
{
    public int? MangaId { get; }
    public string? Title { get; }
    public MediaType? MediaType { get; }

    public DuplicateMangaException(int mangaId)
        : base($"Ya existe una obra con el Id {mangaId}.")
    {
        MangaId = mangaId;
    }

    public DuplicateMangaException(string title)
        : base($"Ya existe una obra con el título '{title}'.")
    {
        Title = title;
    }

    public DuplicateMangaException(string title, MediaType mediaType)
        : base($"Ya existe una obra de tipo {mediaType} con el título '{title}'.")
    {
        Title = title;
        MediaType = mediaType;
    }
}
