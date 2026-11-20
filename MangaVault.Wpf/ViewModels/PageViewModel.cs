using CommunityToolkit.Mvvm.ComponentModel;
using MangaVault.Wpf.Services;

namespace MangaVault.Wpf.ViewModels;

public abstract partial class PageViewModel : ObservableObject
{
    public abstract string Title { get; }
    public abstract string Subtitle { get; }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string? value) =>
        OnPropertyChanged(nameof(HasError));

    public virtual Task ActivateAsync() => Task.CompletedTask;

    protected async Task<bool> RunSafeAsync(
        IBackgroundExecutor executor,
        IAppLogger logger,
        Action work,
        Action<string?>? onError = null,
        bool showLoading = true)
    {
        (bool success, _) = await RunSafeAsync(
            executor,
            logger,
            () =>
            {
                work();
                return true;
            },
            onError,
            showLoading);

        return success;
    }

    protected async Task<(bool Success, T? Value)> RunSafeAsync<T>(
        IBackgroundExecutor executor,
        IAppLogger logger,
        Func<T> work,
        Action<string?>? onError = null,
        bool showLoading = true)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(work);

        if (onError is null)
            ErrorMessage = null;
        else
            onError(null);

        if (showLoading)
            IsLoading = true;

        try
        {
            T result = await executor.RunAsync(work);
            return (true, result);
        }
        catch (Exception ex)
        {
            string friendly = UserMessageTranslator.Translate(ex);
            if (UserMessageTranslator.IsExpected(ex))
                logger.Warning($"Error controlado en {GetType().Name}: {friendly}");
            else
                logger.Error($"Error inesperado en {GetType().Name}.", ex);

            if (onError is null)
                ErrorMessage = friendly;
            else
                onError(friendly);

            return (false, default);
        }
        finally
        {
            if (showLoading)
                IsLoading = false;
        }
    }
}
