using System.Windows;
using System.Windows.Threading;
using MangaVault.Infrastructure.Persistence.Sqlite;
using MangaVault.Wpf.Services;
using MangaVault.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MangaVault.Wpf;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;
    private IAppLogger? _logger;
    private INotificationService? _notifications;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        AppEnvironment environment = AppEnvironment.FromEnvironment();
        var services = new ServiceCollection();
        services.AddMangaVaultServices(environment);

        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<IAppLogger>();
        _notifications = _serviceProvider.GetRequiredService<INotificationService>();

        try
        {
            SqliteDatabase database = _serviceProvider.GetRequiredService<SqliteDatabase>();
            database.Initialize();
            _logger.Info($"Aplicación WPF iniciada. BD SQLite: {database.DatabasePath}");
        }
        catch (Exception ex)
        {
            _logger.Error("Error al inicializar la base de datos SQLite.", ex);
            MessageBox.Show(
                UserMessageTranslator.Translate(ex),
                "MangaVault - Error de inicio",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
            return;
        }

        MainWindow mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        _logger?.Info("Aplicación WPF cerrada.");
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.Error("Excepción no controlada en el hilo de interfaz.", e.Exception);
        string friendly = UserMessageTranslator.Translate(e.Exception);

        if (_notifications is not null)
        {
            _notifications.Show(friendly, NotificationKind.Error);
        }
        else
        {
            MessageBox.Show(friendly, "MangaVault", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        e.Handled = true;
    }
}
