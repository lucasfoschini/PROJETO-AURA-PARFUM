using AuraScent.Controls;
using AuraScent.ViewModels;

namespace AuraScent.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _vm;
    private bool _loaded;

    public HomePage(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;

        _ = HeroBlock.RevealAsync();
        await _vm.LoadCommand.ExecuteAsync(null);
        await CardsBlock.RevealAsync(100);
    }
}
