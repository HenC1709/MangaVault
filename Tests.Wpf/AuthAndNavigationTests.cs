using System.IO;
using MangaVault.Application.DTOs;
using MangaVault.Application.Services;
using MangaVault.Core.Constants;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Core.Interfaces;
using MangaVault.Infrastructure.Persistence.Sqlite;
using MangaVault.Infrastructure.Security;
using MangaVault.Wpf.Services;
using MangaVault.Wpf.ViewModels;
using MangaVault.Wpf.ViewModels.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MangaVault.Wpf.Tests;

public sealed class AuthAndNavigationTests : IDisposable
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
        string dir = Path.Combine(Path.GetTempPath(), $"mangavault-wpf-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        return dir;
    }

    [Fact]
    public async Task Login_ValidCredentials_AuthenticatesAndNavigatesToDashboard()
    {
        using SqliteDatabase database = SqliteDatabase.CreateInMemory();
        database.Initialize();

        var userRepo = new SqliteUserRepository(database);
        var mangaRepo = new SqliteMangaRepository(database);
        var favRepo = new SqliteFavoriteRepository(database);
        var genreRepo = new SqliteGenreRepository(database);
        var hasher = new Pbkdf2PasswordHasher();

        var userService = new UserService(userRepo, hasher);
        var mangaService = new MangaService(mangaRepo);
        var favoriteService = new FavoriteService(favRepo, userRepo, mangaRepo);
        userService.Register(new CreateUserDto("Kirito", "secret123", Role.User));

        var sessionService = new SessionService();
        var executor = new InlineBackgroundExecutor();
        var logger = new InMemoryAppLogger();
        var notification = new NotificationService { Duration = TimeSpan.FromMilliseconds(1) };
        var dialogService = new FakeDialogService();
        var shellLauncher = new FakeShellLauncher();
        var env = AppEnvironment.ForDataDirectory(CreateTempDirectory());

        var services = new ServiceCollection();
        services.AddSingleton(env);
        services.AddSingleton<IAppLogger>(logger);
        services.AddSingleton<ISessionService>(sessionService);
        services.AddSingleton<IBackgroundExecutor>(executor);
        services.AddSingleton<INotificationService>(notification);
        services.AddSingleton(notification);
        services.AddSingleton<IDialogService>(dialogService);
        services.AddSingleton<IShellLauncher>(shellLauncher);
        services.AddSingleton(userService);
        services.AddSingleton(mangaService);
        services.AddSingleton(favoriteService);
        services.AddSingleton<IGenreRepository>(genreRepo);
        services.AddSingleton(new JsonToSqliteImporter(database));
        services.AddSingleton<IPageFactory, ServiceProviderPageFactory>();
        services.AddSingleton<AuthViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<LibraryViewModel>();
        services.AddTransient<FavoritesViewModel>();
        services.AddTransient<SettingsViewModel>();

        using ServiceProvider provider = services.BuildServiceProvider();
        using MainWindowViewModel shell = provider.GetRequiredService<MainWindowViewModel>();

        Assert.False(shell.IsAuthenticated);
        Assert.Null(shell.CurrentPage);

        shell.AuthViewModel.Username = "kirito";
        await shell.AuthViewModel.SubmitCommand.ExecuteAsync(new AuthPasswordInput("secret123"));

        Assert.True(shell.IsAuthenticated);
        Assert.Equal("Kirito", shell.CurrentUserName);
        Assert.Equal(NavigationSection.Dashboard, shell.ActiveSection);
        Assert.IsType<DashboardViewModel>(shell.CurrentPage);
        Assert.True(shell.IsDashboardActive);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShowsFriendlyErrorAndStaysUnauthenticated()
    {
        using SqliteDatabase database = SqliteDatabase.CreateInMemory();
        database.Initialize(seedGenres: false);

        var userService = new UserService(new SqliteUserRepository(database), new Pbkdf2PasswordHasher());
        userService.Register(new CreateUserDto("Asuna", "rapier123"));

        var sessionService = new SessionService();
        var authVm = new AuthViewModel(
            userService,
            sessionService,
            new InlineBackgroundExecutor(),
            new StubNotificationService(),
            new InMemoryAppLogger());

        authVm.Username = "Asuna";
        await authVm.SubmitCommand.ExecuteAsync(new AuthPasswordInput("wrong-pass"));

        Assert.False(sessionService.IsAuthenticated);
        Assert.True(authVm.HasError);
        Assert.Equal(ErrorMessages.InvalidCredentials, authVm.ErrorMessage);
    }

    [Fact]
    public async Task Register_ValidatesMismatchDuplicatesAndAlwaysCreatesStandardUserRole()
    {
        using SqliteDatabase database = SqliteDatabase.CreateInMemory();
        database.Initialize(seedGenres: false);

        var userRepo = new SqliteUserRepository(database);
        var userService = new UserService(userRepo, new Pbkdf2PasswordHasher());
        var sessionService = new SessionService();
        var authVm = new AuthViewModel(
            userService,
            sessionService,
            new InlineBackgroundExecutor(),
            new StubNotificationService(),
            new InMemoryAppLogger());

        authVm.SwitchModeCommand.Execute(true);
        Assert.True(authVm.IsRegisterMode);

        authVm.Username = "Lelouch";
        await authVm.SubmitCommand.ExecuteAsync(new AuthPasswordInput("geass123", "different"));
        Assert.True(authVm.HasError);
        Assert.Equal("Las contraseñas no coinciden.", authVm.ErrorMessage);
        Assert.False(sessionService.IsAuthenticated);

        await authVm.SubmitCommand.ExecuteAsync(new AuthPasswordInput("geass123", "geass123"));
        Assert.False(authVm.HasError);
        Assert.True(sessionService.IsAuthenticated);
        Assert.Equal(Role.User, sessionService.Current!.Role);

        User? stored = userRepo.GetByName("Lelouch");
        Assert.NotNull(stored);
        Assert.Equal(Role.User, stored.Role);

        sessionService.SignOut();
        authVm.SwitchModeCommand.Execute(true);
        authVm.Username = "lelouch";
        await authVm.SubmitCommand.ExecuteAsync(new AuthPasswordInput("otherpass", "otherpass"));
        Assert.True(authVm.HasError);
        Assert.Contains("lelouch", authVm.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MainWindowViewModel_NavigatesAllSectionsAndSignOutResetsState()
    {
        string tempDir = CreateTempDirectory();
        AppEnvironment env = AppEnvironment.ForDataDirectory(tempDir);

        var services = new ServiceCollection();
        services.AddMangaVaultServices(env);
        services.AddSingleton<IBackgroundExecutor, InlineBackgroundExecutor>();

        using ServiceProvider provider = services.BuildServiceProvider();
        SqliteDatabase db = provider.GetRequiredService<SqliteDatabase>();
        db.Initialize();

        UserService userService = provider.GetRequiredService<UserService>();
        userService.Register(new CreateUserDto("Frieren", "magic1000"));

        using MainWindowViewModel shell = provider.GetRequiredService<MainWindowViewModel>();
        shell.AuthViewModel.Username = "Frieren";
        await shell.AuthViewModel.SubmitCommand.ExecuteAsync(new AuthPasswordInput("magic1000"));

        await shell.NavigateAsync(NavigationSection.Library);
        Assert.Equal(NavigationSection.Library, shell.ActiveSection);
        Assert.True(shell.IsLibraryActive);
        Assert.IsType<LibraryViewModel>(shell.CurrentPage);

        await shell.NavigateAsync(NavigationSection.Favorites);
        Assert.Equal(NavigationSection.Favorites, shell.ActiveSection);
        Assert.True(shell.IsFavoritesActive);
        Assert.IsType<FavoritesViewModel>(shell.CurrentPage);

        await shell.NavigateAsync(NavigationSection.Settings);
        Assert.Equal(NavigationSection.Settings, shell.ActiveSection);
        Assert.True(shell.IsSettingsActive);
        Assert.IsType<SettingsViewModel>(shell.CurrentPage);

        shell.SignOutCommand.Execute(null);
        Assert.False(shell.IsAuthenticated);
        Assert.Null(shell.CurrentPage);
        Assert.Equal("", shell.AuthViewModel.Username);
    }

    [Fact]
    public void AppEnvironment_DefaultPathMatchesSqliteDatabaseConvention()
    {
        string expectedDefaultDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MangaVault");

        string? previous = Environment.GetEnvironmentVariable(AppEnvironment.DataDirectoryVariable);
        try
        {
            Environment.SetEnvironmentVariable(AppEnvironment.DataDirectoryVariable, null);
            AppEnvironment env = AppEnvironment.FromEnvironment();

            Assert.Equal(expectedDefaultDir, env.DataDirectory);
            Assert.Equal(SqliteDatabase.GetDefaultDatabasePath(expectedDefaultDir), env.DatabasePath);
            Assert.Equal(Path.Combine(expectedDefaultDir, "logs"), env.LogsDirectory);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AppEnvironment.DataDirectoryVariable, previous);
        }
    }
}
