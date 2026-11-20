using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaVault.Application.Services;
using MangaVault.Core.Constants;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Wpf.Services;
using MangaVault.Wpf.ViewModels.Models;

namespace MangaVault.Wpf.ViewModels;

public sealed partial class FavoritesViewModel : PageViewModel
{
    private readonly FavoriteService _favoriteService;
    private readonly MangaService _mangaService;
    private readonly ISessionService _sessionService;
    private readonly IBackgroundExecutor _executor;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly IAppLogger _logger;

    private bool _hasLoadedOnce;
    private bool _suppressFilterReload;

    public override string Title => "Favoritos";
    public override string Subtitle => "Tus obras marcadas y seguimiento rápido de lectura";

    public IReadOnlyList<FilterOption<ReadingStatus?>> ReadingStatusFilterOptions { get; } =
    [
        new("Todos los estados", null),
        new("Leyendo", ReadingStatus.Reading),
        new("Pendiente", ReadingStatus.PlanToRead),
        new("Terminado", ReadingStatus.Completed),
        new("En pausa", ReadingStatus.OnHold),
        new("Abandonado", ReadingStatus.Dropped)
    ];

    public IReadOnlyList<FilterOption<ReadingStatus>> ReadingStatusOptions { get; } =
    [
        new("Leyendo", ReadingStatus.Reading),
        new("Pendiente", ReadingStatus.PlanToRead),
        new("Terminado", ReadingStatus.Completed),
        new("En pausa", ReadingStatus.OnHold),
        new("Abandonado", ReadingStatus.Dropped)
    ];

    public IReadOnlyList<FilterOption<int?>> RatingOptions { get; } =
    [
        new("Sin calificación", null),
        .. Enumerable.Range(1, 10).Select(n => new FilterOption<int?>($"{n} / 10", n))
    ];

    public ObservableCollection<WorkLibraryItem> Favorites { get; } = [];

    [ObservableProperty]
    private FilterOption<ReadingStatus?> _selectedReadingStatusFilter;

    [ObservableProperty]
    private WorkLibraryItem? _selectedFavorite;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private FilterOption<ReadingStatus> _editReadingStatusOption;

    [ObservableProperty]
    private string _editCurrentChapterText = "0";

    [ObservableProperty]
    private string _editOwnedVolumesText = "0";

    [ObservableProperty]
    private FilterOption<int?> _editRatingOption;

    [ObservableProperty]
    private string? _detailErrorMessage;

    public bool HasSelectedFavorite => SelectedFavorite is not null;
    public bool HasDetailError => !string.IsNullOrWhiteSpace(DetailErrorMessage);

    public FavoritesViewModel(
        FavoriteService favoriteService,
        MangaService mangaService,
        ISessionService sessionService,
        IBackgroundExecutor executor,
        IDialogService dialogService,
        INotificationService notificationService,
        IAppLogger logger)
    {
        _favoriteService = favoriteService ?? throw new ArgumentNullException(nameof(favoriteService));
        _mangaService = mangaService ?? throw new ArgumentNullException(nameof(mangaService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _selectedReadingStatusFilter = ReadingStatusFilterOptions[0];
        _editReadingStatusOption = ReadingStatusOptions[0];
        _editRatingOption = RatingOptions[0];
    }

    partial void OnSelectedReadingStatusFilterChanged(FilterOption<ReadingStatus?> value)
    {
        if (!_suppressFilterReload && _hasLoadedOnce)
            _ = RefreshAsync();
    }

    partial void OnDetailErrorMessageChanged(string? value) =>
        OnPropertyChanged(nameof(HasDetailError));

    partial void OnSelectedFavoriteChanged(WorkLibraryItem? value)
    {
        DetailErrorMessage = null;
        OnPropertyChanged(nameof(HasSelectedFavorite));

        if (value is null)
        {
            EditReadingStatusOption = ReadingStatusOptions[0];
            EditCurrentChapterText = "0";
            EditOwnedVolumesText = "0";
            EditRatingOption = RatingOptions[0];
            return;
        }

        EditReadingStatusOption = ReadingStatusOptions.FirstOrDefault(o => o.Value == value.ReadingStatus)
            ?? ReadingStatusOptions[0];
        EditCurrentChapterText = value.CurrentChapter.ToString(CultureInfo.InvariantCulture);
        EditOwnedVolumesText = value.OwnedVolumes.ToString(CultureInfo.InvariantCulture);
        EditRatingOption = RatingOptions.FirstOrDefault(o => o.Value == value.Rating) ?? RatingOptions[0];
    }

    public override Task ActivateAsync() => RefreshAsync();

    [RelayCommand]
    public Task RefreshAsync() => LoadFavoritesAsync();

    [RelayCommand]
    private Task ClearFilterAsync()
    {
        _suppressFilterReload = true;
        try
        {
            SelectedReadingStatusFilter = ReadingStatusFilterOptions[0];
        }
        finally
        {
            _suppressFilterReload = false;
        }

        return LoadFavoritesAsync();
    }

    public async Task LoadFavoritesAsync(int? preserveWorkId = null)
    {
        Session? session = _sessionService.Current;
        if (session is null)
            return;

        ReadingStatus? statusFilter = SelectedReadingStatusFilter.Value;
        int? targetId = preserveWorkId ?? SelectedFavorite?.Id;

        (bool success, (IReadOnlyList<MangaEntry> Entries, IReadOnlyList<Manga> Works) data) =
            await RunSafeAsync(_executor, _logger, () =>
            {
                IReadOnlyList<MangaEntry> entries = _favoriteService.GetUserFavorites(session.UserId);
                IReadOnlyList<Manga> works = _mangaService.GetAll();
                return (entries, works);
            });

        if (!success)
            return;

        _hasLoadedOnce = true;

        IEnumerable<MangaEntry> filtered = data.Entries;
        if (statusFilter.HasValue)
            filtered = filtered.Where(e => e.ReadingStatus == statusFilter.Value);

        var worksById = data.Works.ToDictionary(w => w.Id);

        Favorites.Clear();
        foreach (MangaEntry entry in filtered.OrderByDescending(e => e.DateAdded))
        {
            if (worksById.TryGetValue(entry.MangaId, out Manga? work))
                Favorites.Add(new WorkLibraryItem(work, entry));
        }

        IsEmpty = Favorites.Count == 0;
        SelectedFavorite = targetId.HasValue
            ? Favorites.FirstOrDefault(f => f.Id == targetId.Value) ?? Favorites.FirstOrDefault()
            : Favorites.FirstOrDefault();
    }

    [RelayCommand]
    private async Task SaveProgressAsync()
    {
        Session? session = _sessionService.Current;
        WorkLibraryItem? target = SelectedFavorite;
        if (session is null || target is null)
            return;

        if (!TryParseNonNegativeInt(EditCurrentChapterText, out int chapter))
        {
            DetailErrorMessage = ErrorMessages.NegativeChapters;
            return;
        }

        if (!TryParseNonNegativeInt(EditOwnedVolumesText, out int ownedVolumes))
        {
            DetailErrorMessage = ErrorMessages.NegativeVolumes;
            return;
        }

        ReadingStatus status = EditReadingStatusOption.Value;
        int? rating = EditRatingOption.Value;
        int workId = target.Id;

        bool success = await RunSafeAsync(
            _executor,
            _logger,
            () =>
            {
                _favoriteService.UpdateProgress(session.UserId, workId, chapter, status);
                _favoriteService.SetOwnedVolumes(session.UserId, workId, ownedVolumes);
                _favoriteService.SetRating(session.UserId, workId, rating);
            },
            msg => DetailErrorMessage = msg);

        if (!success)
            return;

        await LoadFavoritesAsync(workId);
        _notificationService.Show($"Progreso actualizado para '{target.Title}'.");
    }

    [RelayCommand]
    private async Task IncrementChapterAsync(WorkLibraryItem? item)
    {
        Session? session = _sessionService.Current;
        WorkLibraryItem? target = item ?? SelectedFavorite;
        if (session is null || target is null)
            return;

        int nextChapter = target.CurrentChapter + 1;
        int workId = target.Id;

        bool success = await RunSafeAsync(
            _executor,
            _logger,
            () => { _favoriteService.UpdateProgress(session.UserId, workId, nextChapter, target.ReadingStatus); },
            msg => DetailErrorMessage = msg);

        if (!success)
        {
            if (!string.IsNullOrWhiteSpace(DetailErrorMessage))
                _notificationService.Show(DetailErrorMessage, NotificationKind.Warning);
            return;
        }

        await LoadFavoritesAsync(workId);
        _notificationService.Show($"{target.Title}: capítulo {nextChapter}.");
    }

    [RelayCommand]
    private async Task UnfavoriteAsync(WorkLibraryItem? item)
    {
        Session? session = _sessionService.Current;
        WorkLibraryItem? target = item ?? SelectedFavorite;
        if (session is null || target is null)
            return;

        int workId = target.Id;
        bool success = await RunSafeAsync(
            _executor,
            _logger,
            () => { _favoriteService.SetFavorite(session.UserId, workId, false); },
            msg => DetailErrorMessage = msg);

        if (!success)
            return;

        await LoadFavoritesAsync();
        _notificationService.Show($"'{target.Title}' quitada de favoritos.", NotificationKind.Info);
    }

    [RelayCommand]
    private async Task RemoveFromLibraryAsync()
    {
        Session? session = _sessionService.Current;
        WorkLibraryItem? target = SelectedFavorite;
        if (session is null || target is null)
            return;

        bool confirmed = _dialogService.Confirm(
            "Quitar de tu biblioteca",
            $"¿Quitar '{target.Title}' de tu biblioteca? También desaparecerá de Favoritos y se perderá tu progreso.",
            "Quitar",
            isDestructive: true);

        if (!confirmed)
            return;

        int workId = target.Id;
        bool success = await RunSafeAsync(
            _executor,
            _logger,
            () => _favoriteService.RemoveFromLibrary(session.UserId, workId),
            msg => DetailErrorMessage = msg);

        if (!success)
            return;

        await LoadFavoritesAsync();
        _notificationService.Show($"'{target.Title}' quitada de tu biblioteca.", NotificationKind.Info);
    }

    private static bool TryParseNonNegativeInt(string? raw, out int value)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = 0;
            return true;
        }

        return int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= 0;
    }
}
