using System.Globalization;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;

namespace MangaVault.Wpf.ViewModels.Models;

public enum NavigationSection
{
    Dashboard,
    Library,
    Favorites,
    Settings
}

public sealed record FilterOption<T>(string Label, T Value)
{
    public override string ToString() => Label;
}

public sealed record StatusBreakdownItem(string Label, int Count, double Ratio, string AccentKey);

public sealed record UserSummaryItem(int UserId, string Name, Role Role)
{
    public string RoleLabel => DomainLabels.GetRoleLabel(Role);
}

public sealed record AuthPasswordInput(string Password, string? ConfirmPassword = null);

public sealed record ChangePasswordInput(string CurrentPassword, string NewPassword, string ConfirmPassword);

public sealed class WorkLibraryItem
{
    public Manga Manga { get; }
    public MangaEntry? Entry { get; }

    public int Id => Manga.Id;
    public string Title => Manga.Title;
    public string Author => string.IsNullOrWhiteSpace(Manga.Mangaka) ? "Autor desconocido" : Manga.Mangaka;
    public string? Illustrator => Manga.Illustrator;
    public bool HasIllustrator => !string.IsNullOrWhiteSpace(Manga.Illustrator);
    public MediaType Type => Manga.Type;
    public string TypeBadge => DomainLabels.GetMediaTypeLabel(Manga.Type);
    public MangaStatus Status => Manga.Status;
    public string StatusLabel => DomainLabels.GetMangaStatusLabel(Manga.Status);
    public string StatusAccentKey => DomainLabels.GetMangaStatusAccentKey(Manga.Status);
    public int VolumeCount => Manga.VolumeCount;
    public int FinalChapters => Manga.FinalChapters;
    public string? CoverPath => Manga.CoverPath;
    public bool HasCover => !string.IsNullOrWhiteSpace(Manga.CoverPath);
    public string Monogram { get; }
    public string? Synopsis => Manga.Synopsis;
    public bool HasSynopsis => !string.IsNullOrWhiteSpace(Manga.Synopsis);
    public IReadOnlyList<string> Genres { get; }
    public string GenresSummary { get; }

    public bool HasEntry => Entry is not null;
    public bool IsFavorite => Entry?.IsFavorite ?? false;
    public ReadingStatus ReadingStatus => Entry?.ReadingStatus ?? ReadingStatus.PlanToRead;
    public string ReadingStatusLabel => Entry is null
        ? "Sin seguir"
        : DomainLabels.GetReadingStatusLabel(Entry.ReadingStatus);
    public string ReadingStatusAccentKey => Entry is null
        ? "Secondary"
        : DomainLabels.GetReadingStatusAccentKey(Entry.ReadingStatus);
    public int CurrentChapter => Entry?.CurrentChapter ?? 0;
    public int OwnedVolumes => Entry?.OwnedVolumes ?? 0;
    public int? Rating => Entry?.Rating;
    public string RatingDisplay => Rating.HasValue ? $"{Rating.Value}/10" : "Sin calificar";
    public string ProgressDisplay => Entry is null
        ? (FinalChapters > 0 ? $"0 / {FinalChapters} caps" : "No en biblioteca")
        : (FinalChapters > 0 ? $"Cap. {CurrentChapter} / {FinalChapters}" : $"Cap. {CurrentChapter}");
    public string DateAddedDisplay => Entry is null
        ? "—"
        : Entry.DateAdded.ToLocalTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public WorkLibraryItem(Manga manga, MangaEntry? entry = null)
    {
        Manga = manga ?? throw new ArgumentNullException(nameof(manga));
        Entry = entry;
        Genres = manga.Genres.Select(g => g.Name).ToList();
        GenresSummary = Genres.Count == 0 ? "Sin géneros" : string.Join(", ", Genres);
        Monogram = BuildMonogram(manga.Title);
    }

    private static string BuildMonogram(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "MV";

        string[] words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length >= 2)
            return string.Concat(char.ToUpperInvariant(words[0][0]), char.ToUpperInvariant(words[1][0]));

        string first = words[0];
        return first.Length >= 2
            ? string.Concat(char.ToUpperInvariant(first[0]), char.ToUpperInvariant(first[1]))
            : char.ToUpperInvariant(first[0]).ToString();
    }
}

public static class DomainLabels
{
    public static string GetMediaTypeLabel(MediaType type) => type switch
    {
        MediaType.Manga => "Manga",
        MediaType.LightNovel => "Novela ligera",
        _ => type.ToString()
    };

    public static string GetMangaStatusLabel(MangaStatus status) => status switch
    {
        MangaStatus.Ongoing => "En curso",
        MangaStatus.Completed => "Completado",
        MangaStatus.Hiatus => "En pausa",
        MangaStatus.Cancelled => "Cancelado",
        _ => status.ToString()
    };

    public static string GetMangaStatusAccentKey(MangaStatus status) => status switch
    {
        MangaStatus.Ongoing => "Secondary",
        MangaStatus.Completed => "Success",
        MangaStatus.Hiatus => "Warning",
        MangaStatus.Cancelled => "Error",
        _ => "Primary"
    };

    public static string GetReadingStatusLabel(ReadingStatus status) => status switch
    {
        ReadingStatus.Reading => "Leyendo",
        ReadingStatus.PlanToRead => "Pendiente",
        ReadingStatus.Completed => "Terminado",
        ReadingStatus.OnHold => "En pausa",
        ReadingStatus.Dropped => "Abandonado",
        _ => status.ToString()
    };

    public static string GetReadingStatusAccentKey(ReadingStatus status) => status switch
    {
        ReadingStatus.Reading => "Primary",
        ReadingStatus.PlanToRead => "Secondary",
        ReadingStatus.Completed => "Success",
        ReadingStatus.OnHold => "Warning",
        ReadingStatus.Dropped => "Error",
        _ => "Primary"
    };

    public static string GetRoleLabel(Role role) => role switch
    {
        Role.User => "Lector",
        Role.Mangaka => "Mangaka",
        Role.Admin => "Administrador",
        _ => role.ToString()
    };
}
