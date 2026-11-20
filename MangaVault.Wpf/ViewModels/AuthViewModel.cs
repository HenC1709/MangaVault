using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaVault.Application.DTOs;
using MangaVault.Application.Services;
using MangaVault.Core.Constants;
using MangaVault.Core.Entities;
using MangaVault.Core.Enums;
using MangaVault.Wpf.Services;
using MangaVault.Wpf.ViewModels.Models;

namespace MangaVault.Wpf.ViewModels;

public sealed partial class AuthViewModel(
    UserService userService,
    ISessionService sessionService,
    IBackgroundExecutor executor,
    INotificationService notificationService,
    IAppLogger logger) : ObservableObject
{
    private readonly UserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    private readonly ISessionService _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
    private readonly IBackgroundExecutor _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    private readonly INotificationService _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    private readonly IAppLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [ObservableProperty]
    private bool _isRegisterMode;

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public bool IsNotBusy => !IsBusy;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string ModeHeading => IsRegisterMode ? "Crear cuenta nueva" : "Iniciar sesión";

    public string ModeDescription => IsRegisterMode
        ? "Registra un perfil personal para organizar tu catálogo de manga y novelas ligeras."
        : "Accede con tu usuario para consultar tu biblioteca, progreso y favoritos.";

    public string PrimaryButtonText => IsRegisterMode ? "Crear cuenta" : "Entrar";

    public string SwitchModePrompt => IsRegisterMode ? "¿Ya tienes cuenta?" : "¿Primera vez en MangaVault?";

    public string SwitchModeButtonText => IsRegisterMode ? "Iniciar sesión" : "Crear cuenta";

    partial void OnIsRegisterModeChanged(bool value)
    {
        OnPropertyChanged(nameof(ModeHeading));
        OnPropertyChanged(nameof(ModeDescription));
        OnPropertyChanged(nameof(PrimaryButtonText));
        OnPropertyChanged(nameof(SwitchModePrompt));
        OnPropertyChanged(nameof(SwitchModeButtonText));
    }

    partial void OnErrorMessageChanged(string? value) =>
        OnPropertyChanged(nameof(HasError));

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
        SubmitCommand.NotifyCanExecuteChanged();
    }

    private bool CanSubmit() => !IsBusy;

    [RelayCommand]
    private void SwitchMode(bool registerMode)
    {
        IsRegisterMode = registerMode;
        ErrorMessage = null;
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync(AuthPasswordInput? input)
    {
        if (input is null)
        {
            ErrorMessage = ErrorMessages.PasswordRequired;
            return;
        }

        if (IsRegisterMode && !string.Equals(input.Password, input.ConfirmPassword ?? "", StringComparison.Ordinal))
        {
            ErrorMessage = "Las contraseñas no coinciden.";
            return;
        }

        ErrorMessage = null;
        IsBusy = true;

        try
        {
            string username = Username;
            bool registerMode = IsRegisterMode;

            Session session = await _executor.RunAsync(() =>
            {
                if (registerMode)
                {
                    User created = _userService.Register(new CreateUserDto(username, input.Password, Role.User));
                    return new Session(created);
                }

                return _userService.Authenticate(username, input.Password);
            });

            _logger.Info(registerMode
                ? $"Cuenta registrada e iniciada para '{session.UserName}'."
                : $"Inicio de sesión exitoso para '{session.UserName}'.");

            _sessionService.SignIn(session);
            _notificationService.Show(
                registerMode
                    ? $"Bienvenido a MangaVault, {session.UserName}."
                    : $"Sesión iniciada como {session.UserName}.");
        }
        catch (Exception ex)
        {
            string friendly = UserMessageTranslator.Translate(ex);
            if (UserMessageTranslator.IsExpected(ex))
                _logger.Warning($"Autenticación rechazada para '{Username.Trim()}': {friendly}");
            else
                _logger.Error("Error inesperado durante la autenticación.", ex);

            ErrorMessage = friendly;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Reset()
    {
        Username = "";
        ErrorMessage = null;
        IsRegisterMode = false;
        IsBusy = false;
    }
}
