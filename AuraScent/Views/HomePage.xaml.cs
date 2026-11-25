using AuraScent.Controls;
using AuraScent.ViewModels;
using Microsoft.Maui.Layouts;
using System.ComponentModel;

namespace AuraScent.Views;

public partial class HomePage : ContentPage
{
    public static readonly FlexBasis RecommendationColumn = new(0.32f, true);

    private readonly HomeViewModel _vm;
    private Window? _motionWindow;
    private bool _motionActive;
    private bool _loaded;
    private bool _wide;

    public HomePage(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_motionWindow is null && Window is { } window)
        {
            _motionWindow = window;
            window.Stopped += PauseMotion;
            window.Resumed += ResumeMotion;
        }
        _vm.PropertyChanged += OnWeatherRendered;
        ResumeMotion(this, EventArgs.Empty);

        if (_loaded) return;
        _loaded = true;
        HeroContent.Reveal(travel: 8);
        _vm.LoadCommand.Execute(null);
    }

    private void OnWeatherRendered(object? sender, PropertyChangedEventArgs e)
    {
        if (!_motionActive || e.PropertyName != nameof(HomeViewModel.HasWeather)) return;
        WeatherGrid.Reveal();
        AtmospherePanel.Reveal(delay: 65);
        CardsBlock.Reveal(delay: 125);
    }

    internal void FocusCitySearch() => Dispatcher.Dispatch(() => CityEntry.Focus());

    private void FinishCitySearch(object? sender, EventArgs e) => CityEntry.Unfocus();

    protected override void OnDisappearing()
    {
        _vm.PropertyChanged -= OnWeatherRendered;
        if (_motionWindow is not null)
        {
            _motionWindow.Stopped -= PauseMotion;
            _motionWindow.Resumed -= ResumeMotion;
            _motionWindow = null;
        }
        PauseMotion(this, EventArgs.Empty);
        base.OnDisappearing();
    }

    private void PauseMotion(object? sender, EventArgs e)
    {
        _motionActive = false;
        Sky.SetMotionActive(false);
        HeroContent.ResetMotion();
        WeatherGrid.ResetMotion();
        AtmospherePanel.ResetMotion();
        CardsBlock.ResetMotion();
    }

    private void ResumeMotion(object? sender, EventArgs e)
    {
        _motionActive = true;
        Sky.SetMotionActive(true);
    }

    private async void ShowRecommendations(object? sender, EventArgs e)
    {
        await HomeScroll.ScrollToAsync(CardsBlock, ScrollToPosition.Start, Motion.IsEnabled);
        if (Handler is not null) SemanticScreenReader.Announce("Assinaturas para hoje");
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        var wide = width >= 760;
        if (wide == _wide) return;
        _wide = wide;
        PageContent.Padding = wide ? new Thickness(36, 20, 36, 32) : new Thickness(16, 12, 16, 24);
        HeroContent.Padding = wide ? new Thickness(30) : new Thickness(18);
        WeatherGrid.ColumnDefinitions.Clear();
        WeatherGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        if (wide) WeatherGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetRow(WeatherBlock, wide ? 0 : 1);
        Grid.SetColumn(WeatherBlock, wide ? 1 : 0);
        WeatherGrid.Margin = wide ? new Thickness(0, 34, 0, 0) : Thickness.Zero;
        DiscoverButton.IsVisible = wide;

        var mobileLayout = (Layout)MobileCards.Content;
        BindableLayout.SetItemsSource(wide ? mobileLayout : WideCards, null);
        BindableLayout.SetItemsSource(wide ? WideCards : mobileLayout, _vm.Recommendations);
        MobileCards.IsVisible = !wide;
        WideCards.IsVisible = wide;
    }
}
