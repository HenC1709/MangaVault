using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaVault.Application.Services;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Wpf.Services;
using MangaVault.Wpf.ViewModels.Models;

namespace MangaVault.Wpf.ViewModels;

public sealed partial class DashboardViewModel(
    MangaService mangaService,
    FavoriteService favoriteService,
    ISessionService sessionService,
    IBackgroundExecutor executor,
    IAppLogger logger) : PageViewModel
{
    private readonly MangaService _mangaService = mangaService ?? throw new ArgumentNullException(nameof(mangaService));
    private readonly FavoriteService _favoriteService = favoriteService ?? throw new ArgumentNullException(nameof(favoriteService));
    private readonly ISessionService _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
    private readonly IBackgroundExecutor _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    private readonly IAppLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public override string Title => "Dashboard";
    public override string Subtitle => "Resumen real de tu catálogo y tu biblioteca personal";

    [ObservableProperty]
    private int _totalWorks;

    [ObservableProperty]
    private int _mangaCount;

    [ObservableProperty]
    private int _lightNovelCount;

    [ObservableProperty]
    private int _libraryCount;

    [ObservableProperty]
    private int _favoritesCount;

    [ObservableProperty]
    private int _totalOwnedVolumes;

    [ObservableProperty]
    private double? _averageRating;

    [ObservableProperty]
    private string _averageRatingDisplay = "Sin calificar";

    [ObservableProperty]
    private bool _hasNoWorks = true;

    [ObservableProperty]
    private bool _hasNoFavorites = true;

    public ObservableCollection<StatusBreakdownItem> PublicationBreakdown { get; } = [];
    public ObservableCollection<StatusBreakdownItem> ReadingBreakdown { get; } = [];
    public ObservableCollection<WorkLibraryItem> RecentFavorites { get; } = [];

    public override Task ActivateAsync() => RefreshAsync();

    [RelayCommand]
    public async Task RefreshAsync()
    {
        Session? session = _sessionService.Current;
        if (session is null)
            return;

        (bool success, (IReadOnlyList<Manga> Works, IReadOnlyList<MangaEntry> Entries) data) =
            await RunSafeAsync(_executor, _logger, () =>
            {
                IReadOnlyList<Manga> works = _mangaService.GetAll();
                IReadOnlyList<MangaEntry> entries = _favoriteService.GetUserLibrary(session.UserId);
                return (works, entries);
            });

        if (!success)
            return;

        IReadOnlyList<Manga> works = data.Works;
        IReadOnlyList<MangaEntry> entries = data.Entries;

        TotalWorks = works.Count;
        MangaCount = works.Count(w => w.Type == MediaType.Manga);
        LightNovelCount = works.Count(w => w.Type == MediaType.LightNovel);
        HasNoWorks = works.Count == 0;

        LibraryCount = entries.Count;
        FavoritesCount = entries.Count(e => e.IsFavorite);
        TotalOwnedVolumes = entries.Sum(e => e.OwnedVolumes);

        List<int> rated = entries
            .Where(e => e.Rating.HasValue)
            .Select(e => e.Rating!.Value)
            .ToList();

        if (rated.Count > 0)
        {
            double avg = rated.Average();
            AverageRating = avg;
            AverageRatingDisplay = string.Create(CultureInfo.InvariantCulture, $"{avg:0.0} / 10");
        }
        else
        {
            AverageRating = null;
            AverageRatingDisplay = "Sin calificar";
        }

        PublicationBreakdown.Clear();
        foreach (MangaStatus status in Enum.GetValues<MangaStatus>())
        {
            int count = works.Count(w => w.Status == status);
            double ratio = works.Count == 0 ? 0.0 : (double)count / works.Count;
            PublicationBreakdown.Add(new StatusBreakdownItem(
                DomainLabels.GetMangaStatusLabel(status),
                count,
                ratio,
                DomainLabels.GetMangaStatusAccentKey(status)));
        }

        ReadingBreakdown.Clear();
        foreach (ReadingStatus status in Enum.GetValues<ReadingStatus>())
        {
            int count = entries.Count(e => e.ReadingStatus == status);
            double ratio = entries.Count == 0 ? 0.0 : (double)count / entries.Count;
            ReadingBreakdown.Add(new StatusBreakdownItem(
                DomainLabels.GetReadingStatusLabel(status),
                count,
                ratio,
                DomainLabels.GetReadingStatusAccentKey(status)));
        }

        var worksById = works.ToDictionary(w => w.Id);
        RecentFavorites.Clear();
        foreach (MangaEntry entry in entries
                     .Where(e => e.IsFavorite)
                     .OrderByDescending(e => e.DateAdded)
                     .Take(6))
        {
            if (worksById.TryGetValue(entry.MangaId, out Manga? work))
            {
                RecentFavorites.Add(new WorkLibraryItem(work, entry));
            }
        }

        HasNoFavorites = RecentFavorites.Count == 0;
    }
}
