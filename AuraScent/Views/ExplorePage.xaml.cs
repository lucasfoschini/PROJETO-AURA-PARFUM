using AuraScent.ViewModels;

namespace AuraScent.Views;

public partial class ExplorePage : ContentPage
{
    public ExplorePage(ExploreViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
