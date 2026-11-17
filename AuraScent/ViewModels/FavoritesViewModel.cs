using System.Collections.ObjectModel;
using AuraScent.Models;
using AuraScent.Services;
using CommunityToolkit.Mvvm.Input;

namespace AuraScent.ViewModels;

public partial class FavoritesViewModel : BaseViewModel
{
    private readonly IFavoritesService _favorites;

    public ObservableCollection<PerfumeSummary> Items { get; } = new();

    public FavoritesViewModel(IFavoritesService favorites, AtmosphereState state, INavigationService navigation)
        : base(state, navigation) => _favorites = favorites;

    public void Refresh()
    {
        Items.Clear();
        foreach (var p in _favorites.GetAll()) Items.Add(p);
    }

    [RelayCommand]
    private Task OpenPerfumeAsync(PerfumeSummary? perfume) =>
        perfume is null ? Task.CompletedTask : Navigation.OpenDetailAsync(perfume);
}
