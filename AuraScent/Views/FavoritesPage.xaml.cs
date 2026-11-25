using AuraScent.ViewModels;

namespace AuraScent.Views;

public partial class FavoritesPage : ContentPage
{
    private readonly FavoritesViewModel _vm;

    public FavoritesPage(FavoritesViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Refresh();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0) return;
        var span = width >= 1080 ? 3 : width >= 700 ? 2 : 1;
        if (FavoritesLayout.Span == span) return;
        FavoritesLayout.Span = span;
        PageContent.Padding = span > 1 ? new Thickness(36, 20, 36, 0) : new Thickness(16, 12, 16, 0);
    }
}
