using MangaVault.Application.DTOs;
using MangaVault.Application.Services;
using MangaVault.Core.Constants;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Infrastructure.Persistence.Sqlite;
using MangaVault.Infrastructure.Security;
using MangaVault.Wpf.Services;
using MangaVault.Wpf.ViewModels;
using MangaVault.Wpf.ViewModels.Models;
using Xunit;

namespace MangaVault.Wpf.Tests;

public sealed class LibraryAndFavoritesViewModelTests
{
    private static (
        SqliteDatabase Db,
        MangaService MangaService,
        FavoriteService FavoriteService,
        LibraryViewModel LibraryVm,
        FavoritesViewModel FavoritesVm,
        FakeDialogService Dialogs,
        StubNotificationService Notifications) CreateContext()
    {
        SqliteDatabase db = SqliteDatabase.CreateInMemory();
        db.Initialize();

        var userRepo = new SqliteUserRepository(db);
        var mangaRepo = new SqliteMangaRepository(db);
        var favRepo = new SqliteFavoriteRepository(db);
        var genreRepo = new SqliteGenreRepository(db);

        var userService = new UserService(userRepo, new Pbkdf2PasswordHasher());
        var mangaService = new MangaService(mangaRepo);
        var favoriteService = new FavoriteService(favRepo, userRepo, mangaRepo);

        User user = userService.Register(new CreateUserDto("Guts", "berserk99"));
        var sessionService = new SessionService();
        sessionService.SignIn(new Session(user));

        var executor = new InlineBackgroundExecutor();
        var dialogs = new FakeDialogService();
        var notifications = new StubNotificationService();
        var logger = new InMemoryAppLogger();

        var libraryVm = new LibraryViewModel(
            mangaService,
            favoriteService,
            genreRepo,
            sessionService,
            executor,
            dialogs,
            notifications,
            logger);

        var favoritesVm = new FavoritesViewModel(
            favoriteService,
            mangaService,
            sessionService,
            executor,
            dialogs,
            notifications,
            logger);

        return (db, mangaService, favoriteService, libraryVm, favoritesVm, dialogs, notifications);
    }

    [Fact]
    public async Task Library_CreateEditDelete_ValidatesAndHonorsConfirmDialog()
    {
        var (db, mangaService, _, libraryVm, _, dialogs, _) = CreateContext();
        using SqliteDatabase _ = db;

        await libraryVm.ActivateAsync();
        Assert.True(libraryVm.IsEmpty);
        Assert.True(libraryVm.SeededGenres.Count >= 20);

        libraryVm.OpenCreateWorkCommand.Execute(null);
        Assert.True(libraryVm.IsEditorOpen);

        libraryVm.FormTitle = "";
        await libraryVm.SaveWorkCommand.ExecuteAsync(null);
        Assert.True(libraryVm.HasFormError);
        Assert.Equal(ErrorMessages.TitleRequired, libraryVm.FormErrorMessage);
        Assert.True(libraryVm.IsEditorOpen);

        libraryVm.FormTitle = "Spice and Wolf";
        libraryVm.FormAuthor = "Isuna Hasekura";
        libraryVm.FormTypeOption = libraryVm.FormTypeOptions.First(o => o.Value == MediaType.LightNovel);
        libraryVm.FormVolumeCountText = "24";
        libraryVm.FormFinalChaptersText = "0";
        libraryVm.ToggleFormGenreCommand.Execute("Aventura");
        libraryVm.ToggleFormGenreCommand.Execute("Romance");
        await libraryVm.SaveWorkCommand.ExecuteAsync(null);

        Assert.False(libraryVm.IsEditorOpen);
        Assert.False(libraryVm.IsEmpty);
        Assert.Single(libraryVm.Works);
        Assert.NotNull(libraryVm.SelectedWork);
        Assert.Equal("Spice and Wolf", libraryVm.SelectedWork!.Title);
        Assert.Equal(MediaType.LightNovel, libraryVm.SelectedWork.Type);

        // Duplicado mismo título + mismo MediaType
        libraryVm.OpenCreateWorkCommand.Execute(null);
        libraryVm.FormTitle = "spice and wolf";
        libraryVm.FormTypeOption = libraryVm.FormTypeOptions.First(o => o.Value == MediaType.LightNovel);
        await libraryVm.SaveWorkCommand.ExecuteAsync(null);
        Assert.True(libraryVm.HasFormError);
        Assert.True(libraryVm.IsEditorOpen);
        libraryVm.CancelEditorCommand.Execute(null);

        // Editar
        libraryVm.OpenEditSelectedWorkCommand.Execute(null);
        libraryVm.FormTitle = "Spice & Wolf";
        await libraryVm.SaveWorkCommand.ExecuteAsync(null);
        Assert.Equal("Spice & Wolf", libraryVm.SelectedWork!.Title);

        // Eliminar cancelado no borra
        dialogs.NextConfirmResult = false;
        await libraryVm.DeleteSelectedWorkCommand.ExecuteAsync(null);
        Assert.Equal(1, dialogs.ConfirmCalls);
        Assert.True(dialogs.LastConfirmIsDestructive);
        Assert.Single(mangaService.GetAll());

        // Eliminar confirmado sí borra
        dialogs.NextConfirmResult = true;
        await libraryVm.DeleteSelectedWorkCommand.ExecuteAsync(null);
        Assert.Empty(mangaService.GetAll());
        Assert.True(libraryVm.IsEmpty);
    }

    [Fact]
    public async Task Library_SearchAndFilters_FilterByTitleTypeAndGenre()
    {
        var (db, mangaService, _, libraryVm, _, _, _) = CreateContext();
        using SqliteDatabase _ = db;

        mangaService.Create(new CreateMangaDto("Re:Zero", "Tappei Nagatsuki", Type: MediaType.LightNovel, Genres: ["Isekai", "Fantasía"]));
        mangaService.Create(new CreateMangaDto("Re:Zero", "Daichi Matsuse", Type: MediaType.Manga, Genres: ["Isekai"]));
        mangaService.Create(new CreateMangaDto("Vagabond", "Takehiko Inoue", Type: MediaType.Manga, Status: MangaStatus.Hiatus, Genres: ["Histórico"]));

        await libraryVm.ActivateAsync();
        Assert.Equal(3, libraryVm.Works.Count);

        libraryVm.SearchQuery = "zero";
        libraryVm.SelectedMediaTypeOption = libraryVm.MediaTypeOptions.First(o => o.Value == MediaType.LightNovel);
        await libraryVm.SearchCommand.ExecuteAsync(null);
        Assert.Single(libraryVm.Works);
        Assert.Equal(MediaType.LightNovel, libraryVm.Works[0].Type);

        await libraryVm.ClearFiltersCommand.ExecuteAsync(null);
        Assert.Equal(3, libraryVm.Works.Count);

        libraryVm.SelectedGenre = "Histórico";
        await libraryVm.RefreshCommand.ExecuteAsync(null);
        Assert.Single(libraryVm.Works);
        Assert.Equal("Vagabond", libraryVm.Works[0].Title);
    }

    [Fact]
    public async Task LibraryAndFavorites_TrackingValidationAndQuickActions_WorkAgainstSqlite()
    {
        var (db, mangaService, _, libraryVm, favoritesVm, dialogs, _) = CreateContext();
        using SqliteDatabase _ = db;

        Manga monster = mangaService.Create(new CreateMangaDto("Monster", "Naoki Urasawa", VolumeCount: 18, FinalChapters: 162));

        await libraryVm.ActivateAsync();
        Assert.Equal(monster.Id, libraryVm.SelectedWork!.Id);
        Assert.False(libraryVm.SelectedWorkInLibrary);

        // Exceder FinalChapters en SaveTracking debe mostrar ErrorMessages.ChapterExceedsTotal
        libraryVm.EntryCurrentChapterText = "200";
        libraryVm.EntryReadingStatusOption = libraryVm.ReadingStatusOptions.First(o => o.Value == ReadingStatus.Reading);
        libraryVm.EntryIsFavorite = true;
        await libraryVm.SaveTrackingCommand.ExecuteAsync(null);

        Assert.True(libraryVm.HasDetailError);
        Assert.Equal(ErrorMessages.ChapterExceedsTotal, libraryVm.DetailErrorMessage);
        Assert.False(libraryVm.SelectedWorkInLibrary);

        // Guardar válido
        libraryVm.EntryCurrentChapterText = "161";
        libraryVm.EntryOwnedVolumesText = "12";
        libraryVm.EntryRatingOption = libraryVm.RatingOptions.First(o => o.Value == 10);
        libraryVm.EntryIsFavorite = true;
        await libraryVm.SaveTrackingCommand.ExecuteAsync(null);

        Assert.False(libraryVm.HasDetailError);
        Assert.True(libraryVm.SelectedWorkInLibrary);
        Assert.True(libraryVm.SelectedWork!.IsFavorite);
        Assert.Equal(161, libraryVm.SelectedWork.CurrentChapter);
        Assert.Equal(12, libraryVm.SelectedWork.OwnedVolumes);
        Assert.Equal(10, libraryVm.SelectedWork.Rating);

        // Vista de favoritos: +1 cap hasta 162, luego 163 falla por FinalChapters
        await favoritesVm.ActivateAsync();
        Assert.Single(favoritesVm.Favorites);
        Assert.Equal("Monster", favoritesVm.Favorites[0].Title);

        await favoritesVm.IncrementChapterCommand.ExecuteAsync(favoritesVm.Favorites[0]);
        Assert.Equal(162, favoritesVm.Favorites[0].CurrentChapter);

        await favoritesVm.IncrementChapterCommand.ExecuteAsync(favoritesVm.Favorites[0]);
        Assert.True(favoritesVm.HasDetailError);
        Assert.Equal(ErrorMessages.ChapterExceedsTotal, favoritesVm.DetailErrorMessage);
        Assert.Equal(162, favoritesVm.Favorites[0].CurrentChapter);

        // Desmarcar favorito desde FavoritesViewModel
        await favoritesVm.UnfavoriteCommand.ExecuteAsync(favoritesVm.Favorites[0]);
        Assert.True(favoritesVm.IsEmpty);

        // Quitar de biblioteca desde LibraryViewModel
        await libraryVm.RefreshCommand.ExecuteAsync(null);
        Assert.True(libraryVm.SelectedWorkInLibrary);
        Assert.False(libraryVm.SelectedWork!.IsFavorite);

        dialogs.NextConfirmResult = true;
        await libraryVm.RemoveFromLibraryCommand.ExecuteAsync(null);
        Assert.False(libraryVm.SelectedWorkInLibrary);
    }

    [Fact]
    public void FilterOption_ToString_ReturnsLabelOnly()
    {
        var typeOption = new FilterOption<MediaType?>("Todos los tipos", null);
        var ratingOption = new FilterOption<int?>("Sin calificación", null);

        Assert.Equal("Todos los tipos", typeOption.ToString());
        Assert.Equal("Sin calificación", ratingOption.ToString());
    }
}
