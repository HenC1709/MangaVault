using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MangaVault.Wpf.Services;

public enum NotificationKind
{
    Success,
    Info,
    Warning,
    Error
}

public interface INotificationService
{
    void Show(string message, NotificationKind kind = NotificationKind.Success);
}

/// <summary>Notificación tipo "toast" que se muestra en la ventana principal y se oculta sola.</summary>
public sealed partial class NotificationService : ObservableObject, INotificationService
{
    private int _version;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private NotificationKind _kind;

    [ObservableProperty]
    private bool _isVisible;

    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(4);

    public void Show(string message, NotificationKind kind = NotificationKind.Success)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        int version = ++_version;
        Message = message;
        Kind = kind;
        IsVisible = true;
        _ = HideLaterAsync(version);
    }

    [RelayCommand]
    private void Dismiss() => IsVisible = false;

    private async Task HideLaterAsync(int version)
    {
        await Task.Delay(Duration);
        if (version == _version)
            IsVisible = false;
    }
}
