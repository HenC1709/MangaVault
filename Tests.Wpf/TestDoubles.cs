using MangaVault.Wpf.Services;

namespace MangaVault.Wpf.Tests;

internal sealed class InlineBackgroundExecutor : IBackgroundExecutor
{
    public Task<T> RunAsync<T>(Func<T> work) => Task.FromResult(work());
}

internal sealed class FakeDialogService : IDialogService
{
    public bool NextConfirmResult { get; set; } = true;
    public int ConfirmCalls { get; private set; }
    public string? LastConfirmTitle { get; private set; }
    public bool LastConfirmIsDestructive { get; private set; }
    public string? NextPickedImage { get; set; }

    public bool Confirm(string title, string message, string confirmText = "Confirmar", bool isDestructive = false)
    {
        ConfirmCalls++;
        LastConfirmTitle = title;
        LastConfirmIsDestructive = isDestructive;
        return NextConfirmResult;
    }

    public string? PickImageFile() => NextPickedImage;
}

internal sealed class FakeShellLauncher : IShellLauncher
{
    public List<string> OpenedPaths { get; } = [];

    public void OpenFolder(string path) => OpenedPaths.Add(path);
}

internal sealed class InMemoryAppLogger : IAppLogger
{
    public List<(string Level, string Message, Exception? Exception)> Entries { get; } = [];

    public void Info(string message) => Entries.Add(("INFO", message, null));
    public void Warning(string message) => Entries.Add(("WARN", message, null));
    public void Error(string message, Exception? exception = null) => Entries.Add(("ERROR", message, exception));
}

internal sealed class StubNotificationService : INotificationService
{
    public List<(string Message, NotificationKind Kind)> Notifications { get; } = [];

    public void Show(string message, NotificationKind kind = NotificationKind.Success) =>
        Notifications.Add((message, kind));
}
