using System.IO;
using MangaVault.Application.DTOs;
using MangaVault.Application.Services;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Infrastructure.Persistence.Json;
using MangaVault.Infrastructure.Persistence.Sqlite;
using MangaVault.Infrastructure.Security;
using MangaVault.Wpf.Services;
using MangaVault.Wpf.ViewModels;
using MangaVault.Wpf.ViewModels.Models;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MangaVault.Wpf.Tests;

public sealed class DashboardAndSettingsViewModelTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    public void Dispose()
    {
        foreach (string dir in _tempDirs)
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    private string CreateTempDirectory()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"mangavault-wpf-dash-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        return dir;
    }

    [Fact]
    public async Task Dashboard_ComputesRealMetricsAndEmptyStates()
    {
        using SqliteDatabase db = SqliteDatabase.CreateInMemory();
        db.Initialize(seedGenres: false);

        var userRepo = new SqliteUserRepository(db);
        var mangaRepo = new SqliteMangaRepository(db);
        var favRepo = new SqliteFavoriteRepository(db);

        var userService = new UserService(userRepo, new Pbkdf2PasswordHasher());
        var mangaService = new MangaService(mangaRepo);
        var favoriteService = new FavoriteService(favRepo, userRepo, mangaRepo);

        User user = userService.Register(new CreateUserDto("Subaru", "returnbydeath"));
        var sessionService = new SessionService();
        sessionService.SignIn(new Session(user));

        var dashboard = new DashboardViewModel(
            mangaService,
            favoriteService,
            sessionService,
            new InlineBackgroundExecutor(),
            new InMemoryAppLogger());

        await dashboard.ActivateAsync();
        Assert.Equal(0, dashboard.TotalWorks);
        Assert.True(dashboard.HasNoWorks);
        Assert.True(dashboard.HasNoFavorites);
        Assert.Equal("Sin calificar", dashboard.AverageRatingDisplay);

        Manga m1 = mangaService.Create(new CreateMangaDto("Berserk", "Kentaro Miura", Status: MangaStatus.Ongoing, Type: MediaType.Manga));
        Manga m2 = mangaService.Create(new CreateMangaDto("Overlord", "Kugane Maruyama", Status: MangaStatus.Completed, Type: MediaType.LightNovel));

        favoriteService.AddToLibrary(user.UserId, m1.Id, ReadingStatus.Reading, 100, isFavorite: true);
        favoriteService.SetOwnedVolumes(user.UserId, m1.Id, 12);
        favoriteService.SetRating(user.UserId, m1.Id, 10);

        favoriteService.AddToLibrary(user.UserId, m2.Id, ReadingStatus.Completed, 16, isFavorite: false);
        favoriteService.SetOwnedVolumes(user.UserId, m2.Id, 4);
        favoriteService.SetRating(user.UserId, m2.Id, 8);

        await dashboard.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(2, dashboard.TotalWorks);
        Assert.Equal(1, dashboard.MangaCount);
        Assert.Equal(1, dashboard.LightNovelCount);
        Assert.Equal(2, dashboard.LibraryCount);
        Assert.Equal(1, dashboard.FavoritesCount);
        Assert.Equal(16, dashboard.TotalOwnedVolumes);
        Assert.Equal("9.0 / 10", dashboard.AverageRatingDisplay);
        Assert.False(dashboard.HasNoWorks);
        Assert.False(dashboard.HasNoFavorites);
        Assert.Single(dashboard.RecentFavorites);
        Assert.Equal("Berserk", dashboard.RecentFavorites[0].Title);
        Assert.Equal(4, dashboard.PublicationBreakdown.Count);
        Assert.Equal(5, dashboard.ReadingBreakdown.Count);
    }

    [Fact]
    public async Task Settings_PasswordChangeJsonImportAndAdminUserListGuard_WorkCorrectly()
    {
        string tempDir = CreateTempDirectory();
        var env = AppEnvironment.ForDataDirectory(tempDir);

        // Crear JSON heredados reales en tempDir
        var hasher = new Pbkdf2PasswordHasher();
        var jsonUsers = new JsonUserRepository(tempDir);
        var jsonMangas = new JsonMangaRepository(tempDir);
        jsonUsers.Save(User.CreateWithPassword("LegacyUser", "legacy123", hasher, Role.User));
        jsonMangas.Save(new Manga(0, "Dune Novel", "Frank Herbert", 1, 0, type: MediaType.LightNovel));

        using var db = new SqliteDatabase(env.DatabasePath);
        db.Initialize();

        var userRepo = new SqliteUserRepository(db);
        var userService = new UserService(userRepo, hasher);
        var importer = new JsonToSqliteImporter(db);
        var sessionService = new SessionService();
        var shellLauncher = new FakeShellLauncher();
        var notifications = new StubNotificationService();
        var logger = new InMemoryAppLogger();
        var executor = new InlineBackgroundExecutor();

        User regularUser = userService.Register(new CreateUserDto("ReaderOne", "oldpass123", Role.User));
        User adminUser = userService.Register(new CreateUserDto("AdminOne", "adminpass1", Role.Admin));

        // 1. Usuario estándar no ve lista de usuarios
        sessionService.SignIn(new Session(regularUser));
        var settingsRegular = new SettingsViewModel(
            env,
            userService,
            importer,
            sessionService,
            executor,
            shellLauncher,
            notifications,
            logger);

        await settingsRegular.ActivateAsync();
        Assert.False(settingsRegular.IsAdmin);
        Assert.Empty(settingsRegular.RegisteredUsers);
        Assert.True(settingsRegular.HasLegacyJsonFiles);

        // Cambio de contraseña fallido por confirmación distinta
        await settingsRegular.ChangePasswordCommand.ExecuteAsync(new ChangePasswordInput("oldpass123", "newpass456", "other"));
        Assert.True(settingsRegular.HasPasswordError);

        // Cambio de contraseña exitoso
        await settingsRegular.ChangePasswordCommand.ExecuteAsync(new ChangePasswordInput("oldpass123", "newpass456", "newpass456"));
        Assert.False(settingsRegular.HasPasswordError);
        Assert.Equal(regularUser.UserId, userService.Authenticate("ReaderOne", "newpass456").UserId);

        // Importación JSON -> SQLite
        await settingsRegular.ImportJsonCommand.ExecuteAsync(null);
        Assert.True(settingsRegular.HasMigrationSummary);
        Assert.NotNull(userRepo.GetByName("LegacyUser"));

        // 2. Usuario Admin sí ve lista de usuarios (sin contraseñas)
        sessionService.SignIn(new Session(adminUser));
        var settingsAdmin = new SettingsViewModel(
            env,
            userService,
            importer,
            sessionService,
            executor,
            shellLauncher,
            notifications,
            logger);

        await settingsAdmin.ActivateAsync();
        Assert.True(settingsAdmin.IsAdmin);
        Assert.Equal(3, settingsAdmin.RegisteredUsers.Count);

        settingsAdmin.OpenDataFolderCommand.Execute(null);
        Assert.Contains(env.DataDirectory, shellLauncher.OpenedPaths);
    }

    [Fact]
    public void UserMessageTranslator_HidesTechnicalSqliteAndUnexpectedDetails()
    {
        var sqliteEx = new SqliteException("SQLite Error 1: 'no such table: SecretInternalTable'.", 1);
        string sqliteMessage = UserMessageTranslator.Translate(sqliteEx);
        Assert.Equal(UserMessageTranslator.DataAccessError, sqliteMessage);
        Assert.DoesNotContain("SecretInternalTable", sqliteMessage, StringComparison.OrdinalIgnoreCase);

        var unexpectedEx = new InvalidOperationException("Internal connection string Data Source=C:\\secret");
        string unexpectedMessage = UserMessageTranslator.Translate(unexpectedEx);
        Assert.Equal(UserMessageTranslator.UnexpectedError, unexpectedMessage);
        Assert.DoesNotContain("Data Source", unexpectedMessage, StringComparison.OrdinalIgnoreCase);
    }
}
