using AuraScent.Controls;
using AuraScent.ViewModels;
using System.ComponentModel;

namespace AuraScent.Views;

public partial class ExplorePage : ContentPage
{
    private readonly ExploreViewModel _vm;
    private bool _open;
    private bool _revealed;

    public ExplorePage(ExploreViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _open = true;
        _vm.PropertyChanged += OnSearchCompleted;
        RevealHeading(this, EventArgs.Empty);
    }

    protected override void OnDisappearing()
    {
        _open = false;
        _vm.PropertyChanged -= OnSearchCompleted;
        ExploreHeading.ResetMotion();
        ResultsTitle.ResetMotion();
        EmptySearchPanel.ResetMotion();
        base.OnDisappearing();
    }

    private void RevealHeading(object? sender, EventArgs e)
    {
        if (!_open || _revealed || ExploreHeading.Handler is null) return;
        _revealed = true;
        ExploreHeading.Reveal();
    }

    private void OnSearchCompleted(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ExploreViewModel.IsBusy) || _vm.IsBusy || _vm.HasError || !_vm.HasSearched) return;
        VisualElement feedback = _vm.HasNoResults ? EmptySearchPanel : ResultsTitle;
        feedback.Reveal(travel: 8);
    }

    private void FinishSearch(object? sender, EventArgs e) => SearchEntry.Unfocus();

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0) return;
        var span = width >= 1080 ? 3 : width >= 700 ? 2 : 1;
        if (ResultsLayout.Span == span) return;
        ResultsLayout.Span = span;
        PageContent.Padding = span > 1 ? new Thickness(36, 20, 36, 0) : new Thickness(16, 12, 16, 0);
    }
}
