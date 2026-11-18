using System.Collections.ObjectModel;
using AuraScent.Models;
using AuraScent.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AuraScent.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly RecommendationService _recommendations;
    private readonly IWeatherService _weather;
    private readonly IApisExtrasService _extras;

    public ObservableCollection<PerfumeSummary> Recommendations { get; } = new();
    public ObservableCollection<CitySuggestion> CitySuggestions { get; } = new();

    [ObservableProperty] private string _city = "São Paulo";
    [ObservableProperty] private string _temperatureText = "--°";
    [ObservableProperty] private string _weatherSummary = "Sentindo o clima…";
    [ObservableProperty] private bool _showCitySuggestions;
    [ObservableProperty] private string _countryFlagUrl = "";

    private CancellationTokenSource? _citySearchCts;
    private CitySuggestion? _selectedCity;
    private bool _selectingCity;
    private string _currentCountryCode = "";

    public HomeViewModel(RecommendationService recommendations, IWeatherService weather, AtmosphereState state, INavigationService navigation, IApisExtrasService extras)
        : base(state, navigation)
    {
        _recommendations = recommendations;
        _weather = weather;
        _extras = extras;
    }

    public string CurrentCountryCode => _currentCountryCode;

    partial void OnCityChanged(string value)
    {
        _selectedCity = null;
        _citySearchCts?.Cancel();
        CitySuggestions.Clear();
        ShowCitySuggestions = false;

        if (_selectingCity || value.Trim().Length < 2) return;
        _ = LoadCitySuggestionsAsync(value);
    }

    private async Task LoadCitySuggestionsAsync(string query)
    {
        var cts = new CancellationTokenSource();
        var previous = _citySearchCts;
        _citySearchCts = cts;
        previous?.Cancel();

        try
        {
            await Task.Delay(350, cts.Token);
            var matches = await _weather.SearchCitiesAsync(query, cts.Token);
            if (cts.IsCancellationRequested || !string.Equals(query, City, StringComparison.Ordinal)) return;

            foreach (var city in matches) CitySuggestions.Add(city);
            ShowCitySuggestions = CitySuggestions.Count > 0;
        }
        catch (OperationCanceledException) { }
        catch (ApiException)
        {
            if (!cts.IsCancellationRequested) ShowCitySuggestions = false;
        }
        finally
        {
            if (ReferenceEquals(_citySearchCts, cts)) _citySearchCts = null;
            cts.Dispose();
        }
    }

    [RelayCommand]
    private async Task SelectCityAsync(CitySuggestion? city)
    {
        if (city is null) return;

        _selectingCity = true;
        City = city.Name;
        _selectingCity = false;
        _selectedCity = city;
        _citySearchCts?.Cancel();
        CitySuggestions.Clear();
        ShowCitySuggestions = false;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        await RunSafeAsync(async () =>
        {
            var result = _selectedCity is null
                ? await _recommendations.GetAsync(City)
                : await _recommendations.GetAsync(_selectedCity);

            State.Current = result.Atmosphere;
            TemperatureText = $"{Math.Round(result.Weather.TemperatureC)}°";
            WeatherSummary = $"{result.Weather.Description} · {result.Weather.City}";

            _currentCountryCode = result.Weather.CountryCode;
            CountryFlagUrl = _extras.GetCountryFlagUrl(_currentCountryCode);

            Recommendations.Clear();
            foreach (var p in result.Perfumes) Recommendations.Add(p);
        });
    }

    [RelayCommand]
    private Task OpenPerfumeAsync(PerfumeSummary? perfume) =>
        perfume is null ? Task.CompletedTask : Navigation.OpenDetailAsync(perfume);
}
