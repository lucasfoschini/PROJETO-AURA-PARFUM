using AuraScent.Views;

namespace AuraScent;

public sealed record FlyoutDestination(string Title, string Icon, string Route, string Location);

public partial class AppShell : Shell
{
    internal const string ExploreRoute = "//discover/exploreTab/explore";
    internal const string FavoritesRoute = "//discover/favoritesTab/favorites";
    private const string HomeRoute = "//discover/todayTab/home";
    private const string AboutRoute = "//information/aboutTab/about";

    public IReadOnlyList<FlyoutDestination> Destinations { get; } =
    [
        new("Hoje", "aura_home.png", "home", HomeRoute),
        new("Explorar", "aura_explore.png", "explore", ExploreRoute),
        new("Favoritos", "aura_heart.png", "favorites", FavoritesRoute),
        new("Sobre o app", "aura_about.png", "about", AboutRoute)
    ];

    public string ActiveDestination => CurrentItem?.CurrentItem?.CurrentItem?.Route ?? "home";

    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(DetailPage), typeof(DetailPage));
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        OnPropertyChanged(nameof(ActiveDestination));
    }

    private void CloseFlyout(object? sender, EventArgs e) => FlyoutIsPresented = false;

    private async void OpenFromFlyout(object? sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        if (sender is Button { CommandParameter: string location })
            await GoToAsync(location);
    }

    private async void FindCity(object? sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        if (CurrentPage is not HomePage) await GoToAsync(HomeRoute);
        if (CurrentPage is HomePage home) home.FocusCitySearch();
    }
}
