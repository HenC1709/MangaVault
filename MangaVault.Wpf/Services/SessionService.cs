using MangaVault.Core.Entities;

namespace MangaVault.Wpf.Services;

public interface ISessionService
{
    Session? Current { get; }
    bool IsAuthenticated { get; }
    event EventHandler? SessionChanged;
    void SignIn(Session session);
    void SignOut();
}

/// <summary>Sesión en memoria de la app de escritorio (no persiste credenciales).</summary>
public sealed class SessionService : ISessionService
{
    public Session? Current { get; private set; }

    public bool IsAuthenticated => Current is not null;

    public event EventHandler? SessionChanged;

    public void SignIn(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Current = session;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SignOut()
    {
        if (Current is null)
            return;

        Current = null;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
