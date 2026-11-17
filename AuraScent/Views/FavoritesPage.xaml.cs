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
}
