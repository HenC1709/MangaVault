using System.Text.Json.Serialization;
using MangaVault.Core.Enums;

namespace MangaVault.Core.Entities;

public class Manga
{
    public int Id { get; set; }

    [JsonInclude]
    public string Title { get; private set; } = "";

    public MediaType Type
    {
        get;
        set
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Tipo de obra no válido.");
            field = value;
        }
    } = MediaType.Manga;

    public string Mangaka
    {
        get;
        set => field = (value ?? "").Trim();
    } = "";

    public string? Illustrator
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public int VolumeCount
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
        }
    }

    public int FinalChapters
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
        }
    }

    public MangaStatus Status
    {
        get;
        set
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Estado de obra no válido.");
            field = value;
        }
    } = MangaStatus.Ongoing;

    public string? CoverPath
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public string? Synopsis
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [JsonInclude]
    public List<Genre> Genres
    {
        get;
        private set => field = value ?? [];
    } = [];

    public Manga() { }

    public Manga(
        int id,
        string title,
        string mangaka,
        int volumeCount,
        int finalChapters,
        MangaStatus status = MangaStatus.Ongoing,
        MediaType type = MediaType.Manga,
        string? illustrator = null,
        string? coverPath = null,
        string? synopsis = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(id);

        Id = id;
        UpdateDetails(title, mangaka, volumeCount, finalChapters, status, type, illustrator, coverPath, synopsis);
    }

    public void SetTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title.Trim();
    }

    public void UpdateDetails(
        string title,
        string mangaka,
        int volumeCount,
        int finalChapters,
        MangaStatus status,
        MediaType type = MediaType.Manga,
        string? illustrator = null,
        string? coverPath = null,
        string? synopsis = null)
    {
        SetTitle(title);
        Mangaka = mangaka;
        VolumeCount = volumeCount;
        FinalChapters = finalChapters;
        Status = status;
        Type = type;
        Illustrator = illustrator;
        CoverPath = coverPath;
        Synopsis = synopsis;
    }

    public void AddGenre(Genre genre)
    {
        ArgumentNullException.ThrowIfNull(genre);

        if (!Genres.Any(g => string.Equals(g.Name, genre.Name, StringComparison.OrdinalIgnoreCase)))
        {
            Genres.Add(genre);
        }
    }

    public bool RemoveGenre(string genreName)
    {
        if (string.IsNullOrWhiteSpace(genreName))
            return false;

        string trimmed = genreName.Trim();
        return Genres.RemoveAll(g => string.Equals(g.Name, trimmed, StringComparison.OrdinalIgnoreCase)) > 0;
    }
}
