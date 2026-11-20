using MangaVault.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MangaVault.Wpf.Services;

/// <summary>Crea las páginas de una sesión (una instancia nueva por inicio de sesión).</summary>
public interface IPageFactory
{
    T Create<T>() where T : PageViewModel;
}

public sealed class ServiceProviderPageFactory(IServiceProvider provider) : IPageFactory
{
    public T Create<T>() where T : PageViewModel => provider.GetRequiredService<T>();
}
