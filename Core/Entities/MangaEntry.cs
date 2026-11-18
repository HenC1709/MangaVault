using System.Text.Json.Serialization;
using MangaVault.Core.Enums;

namespace MangaVault.Core.Entities;

public class MangaEntry
{
    [JsonInclude]
    public int MangaId { get; private set; }
    [JsonInclude]
    public int UserId { get; private set; }
    [JsonInclude]
    public int CurrentChapter { get; private set; }
    [JsonInclude]
    public ReadingStatus ReadingStatus { get; private set; }
    [JsonInclude]
    public DateTime DateAdded { get; private set; }
    [JsonInclude]
    public int OwnedVolumes { get; private set; }
    [JsonInclude]
    public int? Rating { get; private set; }
    [JsonInclude]
    public bool IsFavorite { get; private set; }

    public MangaEntry() { }

    public MangaEntry(
        int userId,
        int mangaId,
        int currentChapter,
        ReadingStatus status = ReadingStatus.PlanToRead,
        int ownedVolumes = 0,
        int? rating = null,
        bool isFavorite = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(mangaId);
        ArgumentOutOfRangeException.ThrowIfNegative(currentChapter);
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status), "Estado de lectura no válido.");

        UserId = userId;
        MangaId = mangaId;
        CurrentChapter = currentChapter;
        ReadingStatus = status;
        DateAdded = DateTime.UtcNow;

        SetOwnedVolumes(ownedVolumes);
        SetRating(rating);
        SetFavorite(isFavorite);
    }

    public void UpdateProgress(int newChapter, ReadingStatus newStatus, int? maxChapters = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(newChapter);
        if (maxChapters is > 0)
            ArgumentOutOfRangeException.ThrowIfGreaterThan(newChapter, maxChapters.Value);
        if (!Enum.IsDefined(newStatus))
            throw new ArgumentOutOfRangeException(nameof(newStatus), "Estado de lectura no válido.");

        CurrentChapter = newChapter;
        ReadingStatus = newStatus;
    }

    public void SetOwnedVolumes(int ownedVolumes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ownedVolumes);
        OwnedVolumes = ownedVolumes;
    }

    public void SetRating(int? rating)
    {
        if (rating is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(rating), "La calificación debe estar entre 1 y 10.");

        Rating = rating;
    }

    public void SetFavorite(bool isFavorite) => IsFavorite = isFavorite;
}
