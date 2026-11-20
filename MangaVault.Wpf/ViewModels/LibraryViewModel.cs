using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaVault.Application.DTOs;
using MangaVault.Application.Services;
using MangaVault.Core.Constants;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Core.Interfaces;
using MangaVault.Wpf.Services;
using MangaVault.Wpf.ViewModels.Models;

namespace MangaVault.Wpf.ViewModels;

public sealed partial class LibraryViewModel : PageViewModel
{
    public const string AllGenresLabel = "Todos";

    private readonly MangaService _mangaService;
    private readonly FavoriteService _favoriteService;
    private readonly IGenreRepository _genreRepository;
    private readonly ISessionService _sessionService;
    private readonly IBackgroundExecutor _executor;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly IAppLogger _logger;

    private bool _hasLoadedOnce;
    private bool _suppressFilterReload;
    private int _editingWorkId;

    public override string Title => "Biblioteca";
    public override string Subtitle => "Catálogo de manga y novelas ligeras con seguimiento personal";

    public IReadOnlyList<FilterOption<MediaType?>> MediaTypeOptions { get; } =
    [
        new("Todos los tipos", null),
        new("Manga", MediaType.Manga),
        new("Novela ligera", MediaType.LightNovel)
    ];

    public IReadOnlyList<FilterOption<MangaStatus?>> MangaStatusOptions { get; } =
    [
        new("Todos los estados", null),
        new("En curso", MangaStatus.Ongoing),
        new("Completado", MangaStatus.Completed),
        new("En pausa", MangaStatus.Hiatus),
        new("Cancelado", MangaStatus.Cancelled)
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

    public IReadOnlyList<FilterOption<MediaType>> FormTypeOptions { get; } =
    [
        new("Manga", MediaType.Manga),
        new("Novela ligera", MediaType.LightNovel)
    ];

    public IReadOnlyList<FilterOption<MangaStatus>> FormStatusOptions { get; } =
    [
        new("En curso", MangaStatus.Ongoing),
        new("Completado", MangaStatus.Completed),
        new("En pausa", MangaStatus.Hiatus),
        new("Cancelado", MangaStatus.Cancelled)
    ];

    public ObservableCollection<string> AvailableGenres { get; } = [AllGenresLabel];
    public ObservableCollection<string> SeededGenres { get; } = [];
    public ObservableCollection<WorkLibraryItem> Works { get; } = [];

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private FilterOption<MediaType?> _selectedMediaTypeOption;

    [ObservableProperty]
    private FilterOption<MangaStatus?> _selectedStatusOption;

    [ObservableProperty]
    private string _selectedGenre = AllGenresLabel;

    [ObservableProperty]
    private WorkLibraryItem? _selectedWork;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private FilterOption<ReadingStatus> _entryReadingStatusOption;

    [ObservableProperty]
    private string _entryCurrentChapterText = "0";

    [ObservableProperty]
    private string _entryOwnedVolumesText = "0";

    [ObservableProperty]
    private FilterOption<int?> _entryRatingOption;

    [ObservableProperty]
    private bool _entryIsFavorite;

    [ObservableProperty]
    private string? _detailErrorMessage;

    [ObservableProperty]
    private bool _isEditorOpen;

    [ObservableProperty]
    private bool _isEditingExisting;

    [ObservableProperty]
    private string _formTitle = "";

    [ObservableProperty]
    private string _formAuthor = "";

    [ObservableProperty]
    private FilterOption<MediaType> _formTypeOption;

    [ObservableProperty]
    private string _formIllustrator = "";

    [ObservableProperty]
    private string _formVolumeCountText = "0";

    [ObservableProperty]
    private string _formFinalChaptersText = "0";

    [ObservableProperty]
    private FilterOption<MangaStatus> _formStatusOption;

    [ObservableProperty]
    private string _formCoverPath = "";

    [ObservableProperty]
    private string _formSynopsis = "";

    [ObservableProperty]
    private string _formGenresText = "";

    [ObservableProperty]
    private string? _formErrorMessage;

    public bool HasSelectedWork => SelectedWork is not null;
    public bool SelectedWorkInLibrary => SelectedWork?.HasEntry ?? false;
    public string TrackButtonText => SelectedWorkInLibrary ? "Guardar seguimiento" : "Agregar a mi biblioteca";
    public bool HasDetailError => !string.IsNullOrWhiteSpace(DetailErrorMessage);
    public bool HasFormError => !string.IsNullOrWhiteSpace(FormErrorMessage);
    public string EditorTitle => IsEditingExisting ? "Editar obra" : "Nueva obra";
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchQuery)
        || SelectedMediaTypeOption.Value.HasValue
        || SelectedStatusOption.Value.HasValue
        || !string.Equals(SelectedGenre, AllGenresLabel, StringComparison.OrdinalIgnoreCase);

    public LibraryViewModel(
        MangaService mangaService,
        FavoriteService favoriteService,
        IGenreRepository genreRepository,
        ISessionService sessionService,
        IBackgroundExecutor executor,
        IDialogService dialogService,
        INotificationService notificationService,
        IAppLogger logger)
    {
        _mangaService = mangaService ?? throw new ArgumentNullException(nameof(mangaService));
        _favoriteService = favoriteService ?? throw new ArgumentNullException(nameof(favoriteService));
        _genreRepository = genreRepository ?? throw new ArgumentNullException(nameof(genreRepository));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _selectedMediaTypeOption = MediaTypeOptions[0];
        _selectedStatusOption = MangaStatusOptions[0];
        _entryReadingStatusOption = ReadingStatusOptions[1];
        _entryRatingOption = RatingOptions[0];
        _formTypeOption = FormTypeOptions[0];
        _formStatusOption = FormStatusOptions[0];
    }

    partial void OnSearchQueryChanged(string value) =>
        OnPropertyChanged(nameof(HasActiveFilters));

    partial void OnSelectedMediaTypeOptionChanged(FilterOption<MediaType?> value)
    {
        OnPropertyChanged(nameof(HasActiveFilters));
        if (!_suppressFilterReload && _hasLoadedOnce)
            _ = LoadWorksAsync();
    }

    partial void OnSelectedStatusOptionChanged(FilterOption<MangaStatus?> value)
    {
        OnPropertyChanged(nameof(HasActiveFilters));
        if (!_suppressFilterReload && _hasLoadedOnce)
            _ = LoadWorksAsync();
    }

    partial void OnSelectedGenreChanged(string value)
    {
        OnPropertyChanged(nameof(HasActiveFilters));
        if (!_suppressFilterReload && _hasLoadedOnce)
            _ = LoadWorksAsync();
    }

    partial void OnDetailErrorMessageChanged(string? value) =>
        OnPropertyChanged(nameof(HasDetailError));

    partial void OnFormErrorMessageChanged(string? value) =>
        OnPropertyChanged(nameof(HasFormError));

    partial void OnIsEditingExistingChanged(bool value) =>
        OnPropertyChanged(nameof(EditorTitle));

    partial void OnSelectedWorkChanged(WorkLibraryItem? value)
    {
        DetailErrorMessage = null;
        OnPropertyChanged(nameof(HasSelectedWork));
        OnPropertyChanged(nameof(SelectedWorkInLibrary));
        OnPropertyChanged(nameof(TrackButtonText));
        SyncTrackingFieldsFromSelection(value);
    }

    public override Task ActivateAsync() => LoadWorksAsync();

    [RelayCommand]
    public Task RefreshAsync() => LoadWorksAsync();

    [RelayCommand]
    public Task SearchAsync() => LoadWorksAsync();

    [RelayCommand]
    public Task ClearFiltersAsync()
    {
        _suppressFilterReload = true;
        try
        {
            SearchQuery = "";
            SelectedMediaTypeOption = MediaTypeOptions[0];
            SelectedStatusOption = MangaStatusOptions[0];
            SelectedGenre = AllGenresLabel;
        }
        finally
        {
            _suppressFilterReload = false;
        }

        OnPropertyChanged(nameof(HasActiveFilters));
        return LoadWorksAsync();
    }

    public async Task LoadWorksAsync(int? preserveSelectionId = null)
    {
        Session? session = _sessionService.Current;
        if (session is null)
            return;

        string query = SearchQuery;
        MediaType? typeFilter = SelectedMediaTypeOption.Value;
        MangaStatus? statusFilter = SelectedStatusOption.Value;
        string genreFilter = SelectedGenre;
        int? targetSelectionId = preserveSelectionId ?? SelectedWork?.Id;

        (bool success, (IReadOnlyList<Manga> Works, IReadOnlyList<MangaEntry> Entries, List<Genre> Genres) result) =
            await RunSafeAsync(_executor, _logger, () =>
            {
                IReadOnlyList<Manga> works = _mangaService.SearchByTitle(query, typeFilter);
                IReadOnlyList<MangaEntry> entries = _favoriteService.GetUserLibrary(session.UserId);
                List<Genre> genres = _genreRepository.GetAll();
                return (works, entries, genres);
            });

        if (!success)
            return;

        _hasLoadedOnce = true;
        SyncAvailableGenres(result.Genres, genreFilter);

        IEnumerable<Manga> filtered = result.Works;
        if (statusFilter.HasValue)
            filtered = filtered.Where(w => w.Status == statusFilter.Value);

        if (!string.IsNullOrWhiteSpace(genreFilter) && !string.Equals(genreFilter, AllGenresLabel, StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(w =>
                w.Genres.Any(g => string.Equals(g.Name, genreFilter, StringComparison.OrdinalIgnoreCase)));
        }

        var entriesByWorkId = result.Entries.ToDictionary(e => e.MangaId);

        Works.Clear();
        foreach (Manga work in filtered)
        {
            entriesByWorkId.TryGetValue(work.Id, out MangaEntry? entry);
            Works.Add(new WorkLibraryItem(work, entry));
        }

        IsEmpty = Works.Count == 0;

        if (targetSelectionId.HasValue)
        {
            SelectedWork = Works.FirstOrDefault(w => w.Id == targetSelectionId.Value) ?? Works.FirstOrDefault();
        }
        else
        {
            SelectedWork = Works.FirstOrDefault();
        }
    }

    [RelayCommand]
    private async Task SaveTrackingAsync()
    {
        Session? session = _sessionService.Current;
        WorkLibraryItem? work = SelectedWork;
        if (session is null || work is null)
            return;

        if (!TryParseNonNegativeInt(EntryCurrentChapterText, out int chapter))
        {
            DetailErrorMessage = ErrorMessages.NegativeChapters;
            return;
        }

        if (!TryParseNonNegativeInt(EntryOwnedVolumesText, out int ownedVolumes))
        {
            DetailErrorMessage = ErrorMessages.NegativeVolumes;
            return;
        }

        ReadingStatus status = EntryReadingStatusOption.Value;
        int? rating = EntryRatingOption.Value;
        bool isFavorite = EntryIsFavorite;
        bool wasInLibrary = work.HasEntry;
        int workId = work.Id;

        bool success = await RunSafeAsync(
            _executor,
            _logger,
            () =>
            {
                if (!wasInLibrary)
                {
                    _favoriteService.AddToLibrary(session.UserId, workId, status, chapter, isFavorite);
                    if (ownedVolumes > 0)
                        _favoriteService.SetOwnedVolumes(session.UserId, workId, ownedVolumes);
                    if (rating.HasValue)
                        _favoriteService.SetRating(session.UserId, workId, rating);
                }
                else
                {
                    _favoriteService.UpdateProgress(session.UserId, workId, chapter, status);
                    _favoriteService.SetOwnedVolumes(session.UserId, workId, ownedVolumes);
                    _favoriteService.SetRating(session.UserId, workId, rating);
                    _favoriteService.SetFavorite(session.UserId, workId, isFavorite);
                }
            },
            msg => DetailErrorMessage = msg);

        if (!success)
            return;

        await LoadWorksAsync(preserveSelectionId: workId);
        _notificationService.Show(wasInLibrary
            ? $"Seguimiento actualizado para '{work.Title}'."
            : $"'{work.Title}' agregada a tu biblioteca.");
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(WorkLibraryItem? item)
    {
        Session? session = _sessionService.Current;
        WorkLibraryItem? target = item ?? SelectedWork;
        if (session is null || target is null)
            return;

        bool newFavorite = !target.IsFavorite;
        int workId = target.Id;

        bool success = await RunSafeAsync(
            _executor,
            _logger,
            () => { _favoriteService.SetFavorite(session.UserId, workId, newFavorite); },
            msg => DetailErrorMessage = msg);

        if (!success)
            return;

        await LoadWorksAsync(preserveSelectionId: SelectedWork?.Id ?? workId);
        _notificationService.Show(newFavorite
            ? $"'{target.Title}' marcada como favorita."
            : $"'{target.Title}' quitada de favoritos.");
    }

    [RelayCommand]
    private async Task RemoveFromLibraryAsync()
    {
        Session? session = _sessionService.Current;
        WorkLibraryItem? target = SelectedWork;
        if (session is null || target is null || !target.HasEntry)
            return;

        bool confirmed = _dialogService.Confirm(
            "Quitar de tu biblioteca",
            $"¿Quitar '{target.Title}' de tu biblioteca personal? Se eliminará tu progreso y calificación para esta obra.",
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

        await LoadWorksAsync(preserveSelectionId: workId);
        _notificationService.Show($"'{target.Title}' quitada de tu biblioteca.", NotificationKind.Info);
    }

    [RelayCommand]
    private void OpenCreateWork()
    {
        IsEditingExisting = false;
        _editingWorkId = 0;
        FormTitle = "";
        FormAuthor = "";
        FormTypeOption = FormTypeOptions[0];
        FormIllustrator = "";
        FormVolumeCountText = "0";
        FormFinalChaptersText = "0";
        FormStatusOption = FormStatusOptions[0];
        FormCoverPath = "";
        FormSynopsis = "";
        FormGenresText = "";
        FormErrorMessage = null;
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void OpenEditSelectedWork()
    {
        if (SelectedWork is null)
            return;

        Manga work = SelectedWork.Manga;
        IsEditingExisting = true;
        _editingWorkId = work.Id;
        FormTitle = work.Title;
        FormAuthor = work.Mangaka;
        FormTypeOption = FormTypeOptions.FirstOrDefault(o => o.Value == work.Type) ?? FormTypeOptions[0];
        FormIllustrator = work.Illustrator ?? "";
        FormVolumeCountText = work.VolumeCount.ToString(CultureInfo.InvariantCulture);
        FormFinalChaptersText = work.FinalChapters.ToString(CultureInfo.InvariantCulture);
        FormStatusOption = FormStatusOptions.FirstOrDefault(o => o.Value == work.Status) ?? FormStatusOptions[0];
        FormCoverPath = work.CoverPath ?? "";
        FormSynopsis = work.Synopsis ?? "";
        FormGenresText = string.Join(", ", work.Genres.Select(g => g.Name));
        FormErrorMessage = null;
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void CancelEditor()
    {
        IsEditorOpen = false;
        FormErrorMessage = null;
    }

    [RelayCommand]
    private void BrowseCover()
    {
        string? file = _dialogService.PickImageFile();
        if (!string.IsNullOrWhiteSpace(file))
            FormCoverPath = file;
    }

    [RelayCommand]
    private void ToggleFormGenre(string? genreName)
    {
        if (string.IsNullOrWhiteSpace(genreName))
            return;

        string target = genreName.Trim();
        List<string> current = FormGenresText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        int existingIndex = current.FindIndex(g => string.Equals(g, target, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
            current.RemoveAt(existingIndex);
        else
            current.Add(target);

        FormGenresText = string.Join(", ", current);
    }

    [RelayCommand]
    private async Task SaveWorkAsync()
    {
        if (!TryParseNonNegativeInt(FormVolumeCountText, out int volumeCount))
        {
            FormErrorMessage = ErrorMessages.NegativeVolumes;
            return;
        }

        if (!TryParseNonNegativeInt(FormFinalChaptersText, out int finalChapters))
        {
            FormErrorMessage = ErrorMessages.NegativeChapters;
            return;
        }

        string[] genres = FormGenresText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var dto = new CreateMangaDto(
            Title: FormTitle,
            Mangaka: FormAuthor,
            VolumeCount: volumeCount,
            FinalChapters: finalChapters,
            Status: FormStatusOption.Value,
            Type: FormTypeOption.Value,
            Illustrator: FormIllustrator,
            CoverPath: FormCoverPath,
            Synopsis: FormSynopsis,
            Genres: genres);

        bool editing = IsEditingExisting;
        int workId = _editingWorkId;

        (bool success, Manga? saved) = await RunSafeAsync(
            _executor,
            _logger,
            () => editing
                ? _mangaService.Update(workId, dto)
                : _mangaService.Create(dto),
            msg => FormErrorMessage = msg);

        if (!success || saved is null)
            return;

        IsEditorOpen = false;
        await LoadWorksAsync(preserveSelectionId: saved.Id);
        _notificationService.Show(editing
            ? $"'{saved.Title}' actualizada correctamente."
            : $"'{saved.Title}' agregada al catálogo.");
    }

    [RelayCommand]
    private async Task DeleteSelectedWorkAsync()
    {
        if (SelectedWork is null)
            return;

        Manga work = SelectedWork.Manga;
        bool confirmed = _dialogService.Confirm(
            "Eliminar obra del catálogo",
            $"¿Eliminar '{work.Title}' de forma permanente? También se quitará de las bibliotecas de todos los usuarios.",
            "Eliminar",
            isDestructive: true);

        if (!confirmed)
            return;

        bool success = await RunSafeAsync(
            _executor,
            _logger,
            () => _mangaService.Delete(work.Id),
            msg => DetailErrorMessage = msg);

        if (!success)
            return;

        await LoadWorksAsync();
        _notificationService.Show($"'{work.Title}' eliminada del catálogo.", NotificationKind.Info);
    }

    private void SyncAvailableGenres(List<Genre> genres, string previousGenre)
    {
        _suppressFilterReload = true;
        try
        {
            AvailableGenres.Clear();
            AvailableGenres.Add(AllGenresLabel);
            SeededGenres.Clear();

            foreach (Genre genre in genres)
            {
                AvailableGenres.Add(genre.Name);
                SeededGenres.Add(genre.Name);
            }

            SelectedGenre = AvailableGenres.FirstOrDefault(
                g => string.Equals(g, previousGenre, StringComparison.OrdinalIgnoreCase))
                ?? AllGenresLabel;
        }
        finally
        {
            _suppressFilterReload = false;
        }
    }

    private void SyncTrackingFieldsFromSelection(WorkLibraryItem? item)
    {
        if (item is null)
        {
            EntryReadingStatusOption = ReadingStatusOptions[1];
            EntryCurrentChapterText = "0";
            EntryOwnedVolumesText = "0";
            EntryRatingOption = RatingOptions[0];
            EntryIsFavorite = false;
            return;
        }

        EntryReadingStatusOption = ReadingStatusOptions.FirstOrDefault(o => o.Value == item.ReadingStatus)
            ?? ReadingStatusOptions[1];
        EntryCurrentChapterText = item.CurrentChapter.ToString(CultureInfo.InvariantCulture);
        EntryOwnedVolumesText = item.OwnedVolumes.ToString(CultureInfo.InvariantCulture);
        EntryRatingOption = RatingOptions.FirstOrDefault(o => o.Value == item.Rating) ?? RatingOptions[0];
        EntryIsFavorite = item.IsFavorite;
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
