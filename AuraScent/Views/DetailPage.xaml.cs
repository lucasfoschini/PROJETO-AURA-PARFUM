using AuraScent.Controls;
using AuraScent.ViewModels;

namespace AuraScent.Views;

public partial class DetailPage : ContentPage
{
    public DetailPage(DetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = HeroBlock.RevealAsync();
        await LayersBlock.RevealAsync(180);
    }
}
