using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaVault.Application.Services;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Infrastructure.Persistence.Sqlite;
using MangaVault.Wpf.Services;
using MangaVault.Wpf.ViewModels.Models;

namespace MangaVault.Wpf.ViewModels;

public sealed partial class SettingsViewModel : PageViewModel
{
    private readonly AppEnvironment _environment;
    private readonly UserService _userService;
    private readonly JsonToSqliteImporter _importer;
    private readonly ISessionService _sessionService;
    private readonly IBackgroundExecutor _executor;
    private readonly IShellLauncher _shellLauncher;
    private readonly INotificationService _notificationService;
    private readonly IAppLogger _logger;

    public override string Title => "Configuración";
    public override string Subtitle => "Cuenta activa, almacenamiento SQLite y migración de datos";

    public string DataDirectory => _environment.DataDirectory;
    public string DatabasePath => _environment.DatabasePath;
    public string LogsDirectory => _environment.LogsDirectory;

    [ObservableProperty]
    private string _userName = "";

    [ObservableProperty]
    private string _userRoleLabel = "";

    [ObservableProperty]
    private string _sessionStartedDisplay = "";

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    private bool _hasLegacyJsonFiles;

    [ObservableProperty]
    private string? _migrationSummary;

    [ObservableProperty]
    private string? _passwordErrorMessage;

    public bool HasMigrationSummary => !string.IsNullOrWhiteSpace(MigrationSummary);
    public bool HasPasswordError => !string.IsNullOrWhiteSpace(PasswordErrorMessage);
    public ObservableCollection<UserSummaryItem> RegisteredUsers { get; } = [];

    public SettingsViewModel(
        AppEnvironment environment,
        UserService userService,
        JsonToSqliteImporter importer,
        ISessionService sessionService,
        IBackgroundExecutor executor,
        IShellLauncher shellLauncher,
        INotificationService notificationService,
        IAppLogger logger)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _importer = importer ?? throw new ArgumentNullException(nameof(importer));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _shellLauncher = shellLauncher ?? throw new ArgumentNullException(nameof(shellLauncher));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    partial void OnMigrationSummaryChanged(string? value) =>
        OnPropertyChanged(nameof(HasMigrationSummary));

    partial void OnPasswordErrorMessageChanged(string? value) =>
        OnPropertyChanged(nameof(HasPasswordError));

    public override async Task ActivateAsync()
    {
        Session? session = _sessionService.Current;
        if (session is null)
            return;

        UserName = session.UserName;
        UserRoleLabel = DomainLabels.GetRoleLabel(session.Role);
        SessionStartedDisplay = session.StartedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        IsAdmin = session.Role == Role.Admin;
        PasswordErrorMessage = null;

        bool includeUsers = session.Role == Role.Admin;

        (bool success, (bool HasJson, List<UserSummaryItem> Users) result) =
            await RunSafeAsync(_executor, _logger, () =>
            {
                bool hasJson = JsonToSqliteImporter.HasJsonDataFiles(_environment.DataDirectory);
                List<UserSummaryItem> users = includeUsers
                    ? _userService.GetAll().Select(u => new UserSummaryItem(u.UserId, u.Name, u.Role)).ToList()
                    : [];

                return (hasJson, users);
            });

        if (!success)
            return;

        HasLegacyJsonFiles = result.HasJson;
        RegisteredUsers.Clear();
        if (includeUsers)
        {
            foreach (UserSummaryItem user in result.Users)
            {
                RegisteredUsers.Add(user);
            }
        }
    }

    [RelayCommand]
    private void OpenDataFolder() => OpenFolderSafe(_environment.DataDirectory, "carpeta de datos");

    [RelayCommand]
    private void OpenLogsFolder() => OpenFolderSafe(_environment.LogsDirectory, "carpeta de registros");

    [RelayCommand]
    private async Task ImportJsonAsync()
    {
        Session? session = _sessionService.Current;
        if (session is null)
            return;

        if (!JsonToSqliteImporter.HasJsonDataFiles(_environment.DataDirectory))
        {
            MigrationSummary = "No se encontraron archivos JSON heredados (users.json, mangas.json, favorites.json) en la carpeta de datos.";
            HasLegacyJsonFiles = false;
            return;
        }

        (bool success, JsonImportResult? result) = await RunSafeAsync(_executor, _logger, () =>
            _importer.Import(_environment.DataDirectory));

        if (!success || result is null)
            return;

        MigrationSummary =
            $"Usuarios: {result.UsersImported} importados ({result.UsersSkipped} omitidos) · " +
            $"Obras: {result.WorksImported} importadas ({result.WorksSkipped} omitidas) · " +
            $"Biblioteca: {result.EntriesImported} importados ({result.EntriesSkipped} omitidos).";

        _logger.Info($"Migración JSON -> SQLite ejecutada: {MigrationSummary}");
        _notificationService.Show(result.HasImportedAny
            ? "Datos JSON importados a SQLite correctamente."
            : "No había registros nuevos para importar desde JSON.",
            result.HasImportedAny ? NotificationKind.Success : NotificationKind.Info);

        if (session.Role == Role.Admin)
            await ActivateAsync();
    }

    [RelayCommand]
    private async Task ChangePasswordAsync(ChangePasswordInput? input)
    {
        Session? session = _sessionService.Current;
        if (session is null || input is null)
            return;

        if (!string.Equals(input.NewPassword, input.ConfirmPassword, StringComparison.Ordinal))
        {
            PasswordErrorMessage = "La confirmación de la nueva contraseña no coincide.";
            return;
        }

        PasswordErrorMessage = null;
        bool success = await RunSafeAsync(
            _executor,
            _logger,
            () => _userService.ChangePassword(session.UserId, input.CurrentPassword, input.NewPassword),
            msg => PasswordErrorMessage = msg);

        if (!success)
            return;

        _logger.Info($"Contraseña actualizada para '{session.UserName}'.");
        _notificationService.Show("Contraseña actualizada correctamente.");
    }

    [RelayCommand]
    private void SignOut() => _sessionService.SignOut();

    private void OpenFolderSafe(string path, string description)
    {
        try
        {
            _shellLauncher.OpenFolder(path);
        }
        catch (Exception ex)
        {
            _logger.Error($"No se pudo abrir la {description}: {path}", ex);
            ErrorMessage = $"No se pudo abrir la {description}. Ruta: {path}";
        }
    }
}
