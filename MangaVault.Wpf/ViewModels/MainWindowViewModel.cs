using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MangaVault.Core.Entities;
using MangaVault.Wpf.Services;
using MangaVault.Wpf.ViewModels.Models;

namespace MangaVault.Wpf.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly ISessionService _sessionService;
    private readonly IPageFactory _pageFactory;
    private readonly IAppLogger _logger;
    private readonly Dictionary<NavigationSection, PageViewModel> _pages = [];
    private bool _disposed;

    public NotificationService Notification { get; }
    public AuthViewModel AuthViewModel { get; }

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string _currentUserName = "";

    [ObservableProperty]
    private string _currentUserRole = "";

    [ObservableProperty]
    private string _currentUserInitial = "M";

    [ObservableProperty]
    private NavigationSection _activeSection = NavigationSection.Dashboard;

    [ObservableProperty]
    private PageViewModel? _currentPage;

    public bool IsDashboardActive => ActiveSection == NavigationSection.Dashboard;
    public bool IsLibraryActive => ActiveSection == NavigationSection.Library;
    public bool IsFavoritesActive => ActiveSection == NavigationSection.Favorites;
    public bool IsSettingsActive => ActiveSection == NavigationSection.Settings;

    public MainWindowViewModel(
        ISessionService sessionService,
        IPageFactory pageFactory,
        AuthViewModel authViewModel,
        NotificationService notificationService,
        IAppLogger logger)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _pageFactory = pageFactory ?? throw new ArgumentNullException(nameof(pageFactory));
        AuthViewModel = authViewModel ?? throw new ArgumentNullException(nameof(authViewModel));
        Notification = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _sessionService.SessionChanged += OnSessionChanged;

        if (_sessionService.Current is not null)
        {
            InitializeAuthenticatedSession(_sessionService.Current);
        }
    }

    partial void OnActiveSectionChanged(NavigationSection value)
    {
        OnPropertyChanged(nameof(IsDashboardActive));
        OnPropertyChanged(nameof(IsLibraryActive));
        OnPropertyChanged(nameof(IsFavoritesActive));
        OnPropertyChanged(nameof(IsSettingsActive));
    }

    [RelayCommand]
    public async Task NavigateAsync(NavigationSection section)
    {
        if (!IsAuthenticated || !_pages.TryGetValue(section, out PageViewModel? page))
            return;

        ActiveSection = section;
        CurrentPage = page;
        await page.ActivateAsync();
    }

    [RelayCommand]
    private void SignOut()
    {
        if (!IsAuthenticated)
            return;

        _logger.Info($"Cierre de sesión para '{CurrentUserName}'.");
        _sessionService.SignOut();
        Notification.Show("Sesión cerrada.", NotificationKind.Info);
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        Session? session = _sessionService.Current;
        if (session is null)
        {
            IsAuthenticated = false;
            CurrentUserName = "";
            CurrentUserRole = "";
            CurrentUserInitial = "M";
            _pages.Clear();
            CurrentPage = null;
            ActiveSection = NavigationSection.Dashboard;
            AuthViewModel.Reset();
            return;
        }

        InitializeAuthenticatedSession(session);
    }

    private void InitializeAuthenticatedSession(Session session)
    {
        _pages.Clear();
        _pages[NavigationSection.Dashboard] = _pageFactory.Create<DashboardViewModel>();
        _pages[NavigationSection.Library] = _pageFactory.Create<LibraryViewModel>();
        _pages[NavigationSection.Favorites] = _pageFactory.Create<FavoritesViewModel>();
        _pages[NavigationSection.Settings] = _pageFactory.Create<SettingsViewModel>();

        CurrentUserName = session.UserName;
        CurrentUserRole = DomainLabels.GetRoleLabel(session.Role);
        CurrentUserInitial = string.IsNullOrWhiteSpace(session.UserName)
            ? "M"
            : char.ToUpperInvariant(session.UserName.Trim()[0]).ToString();
        IsAuthenticated = true;

        _ = NavigateAsync(NavigationSection.Dashboard);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _sessionService.SessionChanged -= OnSessionChanged;
    }
}
