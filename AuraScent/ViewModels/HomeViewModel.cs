using System.Collections.ObjectModel;
using AuraScent.Models;
using AuraScent.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AuraScent.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly RecommendationService _recommendations;

    public ObservableCollection<PerfumeSummary> Recommendations { get; } = new();

    [ObservableProperty] private string _city = "São Paulo";
    [ObservableProperty] private string _temperatureText = "--°";
    [ObservableProperty] private string _weatherSummary = "Sentindo o clima…";

    public HomeViewModel(RecommendationService recommendations, AtmosphereState state, INavigationService navigation)
        : base(state, navigation) => _recommendations = recommendations;

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        await RunSafeAsync(async () =>
        {
            var result = await _recommendations.GetAsync(City);

            State.Current = result.Atmosphere;
            TemperatureText = $"{Math.Round(result.Weather.TemperatureC)}°";
            WeatherSummary = $"{result.Weather.Description} · {result.Weather.City}";

            Recommendations.Clear();
            foreach (var p in result.Perfumes) Recommendations.Add(p);
        });
    }

    [RelayCommand]
    private Task OpenPerfumeAsync(PerfumeSummary? perfume) =>
        perfume is null ? Task.CompletedTask : Navigation.OpenDetailAsync(perfume);
}
