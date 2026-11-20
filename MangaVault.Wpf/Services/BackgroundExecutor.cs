namespace MangaVault.Wpf.Services;

/// <summary>
/// Ejecuta trabajo fuera del hilo de interfaz. Los servicios de Application y los repositorios
/// SQLite son síncronos, así que este es el ÚNICO punto donde se descarga trabajo al ThreadPool.
/// </summary>
public interface IBackgroundExecutor
{
    Task<T> RunAsync<T>(Func<T> work);
}

public sealed class ThreadPoolBackgroundExecutor : IBackgroundExecutor
{
    public Task<T> RunAsync<T>(Func<T> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        return Task.Run(work);
    }
}
