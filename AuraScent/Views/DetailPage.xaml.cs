using AuraScent.Controls;
using AuraScent.ViewModels;

namespace AuraScent.Views;

public partial class DetailPage : ContentPage
{
    private Window? _motionWindow;
    private bool _motionActive;
    private bool _wide;
    private bool _revealed;

    public DetailPage(DetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
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
        ResumeMotion(this, EventArgs.Empty);
        if (_revealed) return;
        _revealed = true;
        BottlePanel.Reveal(travel: 14);
        IdentityBlock.Reveal(delay: 75);
    }

    protected override void OnDisappearing()
    {
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
        Bottle.AnimateLiquid = false;
        BottlePanel.ResetMotion();
        IdentityBlock.ResetMotion();
        LayersHeading.ResetMotion();
        FavoriteButton.ResetMotion();
    }

    private void ResumeMotion(object? sender, EventArgs e)
    {
        _motionActive = true;
        UpdateBottleMotion(DetailScroll.ScrollY);
    }

    private void UpdateVisibleMotion(object? sender, ScrolledEventArgs e) => UpdateBottleMotion(e.ScrollY);

    private void UpdateBottleMotion(double scrollY) => Bottle.AnimateLiquid = _motionActive &&
        (BottlePanel.Height <= 0 || scrollY < HeroBlock.Y + BottlePanel.Y + BottlePanel.Height);

    private async void ShowLayers(object? sender, EventArgs e)
    {
        await DetailScroll.ScrollToAsync(LayersBlock, ScrollToPosition.Start, Motion.IsEnabled);
        if (!_motionActive) return;
        LayersHeading.Reveal(travel: 8);
        SemanticScreenReader.Announce("Pirâmide olfativa");
    }

    private async void AnimateFavorite(object? sender, EventArgs e)
    {
        if (!Motion.IsEnabled) return;
        FavoriteButton.CancelAnimations();
        if (!await FavoriteButton.ScaleToAsync(1.045, 100, Easing.CubicOut))
            await FavoriteButton.ScaleToAsync(1, 120, Easing.CubicOut);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        var wide = width >= 760;
        if (_wide == wide) return;
        _wide = wide;
        PageContent.Padding = wide ? new Thickness(36, 20, 36, 32) : new Thickness(16, 12, 16, 28);
        HeroBlock.ColumnDefinitions.Clear();
        HeroBlock.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        if (wide) HeroBlock.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        Grid.SetRow(IdentityBlock, wide ? 0 : 1);
        Grid.SetColumn(IdentityBlock, wide ? 1 : 0);
        Bottle.HeightRequest = wide ? 338 : 256;
    }
}
