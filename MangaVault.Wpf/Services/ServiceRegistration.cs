using MangaVault.Application.Services;
using MangaVault.Core.Interfaces;
using MangaVault.Infrastructure.Persistence.Sqlite;
using MangaVault.Infrastructure.Security;
using MangaVault.Wpf.ViewModels;
using MangaVault.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MangaVault.Wpf.Services;

public static class ServiceRegistration
{
    public static IServiceCollection AddMangaVaultServices(
        this IServiceCollection services,
        AppEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton(environment);
        services.AddSingleton<IAppLogger>(_ => new FileAppLogger(environment.LogsDirectory));
        services.AddSingleton(_ => new SqliteDatabase(environment.DatabasePath));

        services.AddSingleton<IUserRepository, SqliteUserRepository>();
        services.AddSingleton<IMangaRepository, SqliteMangaRepository>();
        services.AddSingleton<IFavoriteRepository, SqliteFavoriteRepository>();
        services.AddSingleton<IGenreRepository, SqliteGenreRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<JsonToSqliteImporter>();

        services.AddSingleton<UserService>();
        services.AddSingleton<MangaService>();
        services.AddSingleton<FavoriteService>();

        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IBackgroundExecutor, ThreadPoolBackgroundExecutor>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<NotificationService>());
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IShellLauncher, ShellLauncher>();
        services.AddSingleton<IPageFactory, ServiceProviderPageFactory>();

        services.AddSingleton<AuthViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<LibraryViewModel>();
        services.AddTransient<FavoritesViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services;
    }
}
