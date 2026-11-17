using System.Collections.ObjectModel;
using AuraScent.Models;
using AuraScent.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AuraScent.ViewModels;

public partial class DetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IPerfumeService _perfumes;
    private readonly IFavoritesService _favorites;
    private PerfumeDetail? _detail;

    public ObservableCollection<NoteLayer> Layers { get; } = new();

    [ObservableProperty] private PerfumeSummary? _summary;
    [ObservableProperty] private string _lede = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(FavoriteLabel))]
    private bool _isFavorite;

    public string FavoriteLabel => IsFavorite ? "Nos favoritos" : "Favoritar";

    public DetailViewModel(IPerfumeService perfumes, IFavoritesService favorites, AtmosphereState state, INavigationService navigation)
        : base(state, navigation)
    {
        _perfumes = perfumes;
        _favorites = favorites;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Perfume", out var value) && value is PerfumeSummary perfume && perfume != Summary)
        {
            Summary = perfume;
            IsFavorite = _favorites.IsFavorite(perfume);
            LoadCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (Summary is null) return;
        await RunSafeAsync(async () =>
        {
            _detail = await _perfumes.GetDetailAsync(Summary);
            Lede = _detail.Lede;
            Layers.Clear();
            foreach (var layer in _detail.Layers) Layers.Add(layer);
        });
    }

    [RelayCommand] private Task BackAsync() => Navigation.CloseAsync();

    [RelayCommand]
    private void ToggleFavorite()
    {
        if (Summary is not null) IsFavorite = _favorites.Toggle(Summary);
    }

    [RelayCommand]
    private Task EnterRitualAsync()
    {
        if (Summary is null) return Task.CompletedTask;
        var detail = _detail ?? new PerfumeDetail(Summary, "", Array.Empty<NoteLayer>(), null);
        return Navigation.OpenRitualAsync(detail);
    }

    [RelayCommand]
    private async Task OpenScentoAsync()
    {
        var url = _detail?.ScentoUrl ?? Summary?.PageUrl;
        if (!string.IsNullOrWhiteSpace(url)) await Launcher.Default.OpenAsync(url);
    }
}
