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
    private CancellationTokenSource? _citySearchCts;
    private CancellationTokenSource? _loadCts;
    private CitySuggestion? _selectedCity;
    private WeatherSnapshot? _displayedWeather;
    private bool _selectingCity;

    public ObservableCollection<PerfumeSummary> Recommendations { get; } = new();
    public ObservableCollection<CitySuggestion> CitySuggestions { get; } = new();

    [ObservableProperty] private string _city = "São Paulo";
    [ObservableProperty] private string _temperatureText = "";
    [ObservableProperty] private string _weatherSummary = "";
    [ObservableProperty] private bool _showCitySuggestions;
    [ObservableProperty] private string _countryFlagUrl = "";
    public string? DisplayCity => _displayedWeather?.City;
    public string? DisplayCountry => _displayedWeather?.Country;
    public string CurrentCountryCode => _displayedWeather?.CountryCode ?? "";
    public string AtmosphereTitle => NoteTranslator.Capitalize(State.Current.Name);
    public bool HasWeather => _displayedWeather is not null;
    public string LoadingText => HasWeather ? "Atualizando o clima e a curadoria…" : "Consultando o clima e a curadoria…";

    public HomeViewModel(RecommendationService recommendations, IWeatherService weather, AtmosphereState state,
        INavigationService navigation, IApisExtrasService extras)
        : base(state, navigation)
    {
        _recommendations = recommendations;
        _weather = weather;
        _extras = extras;
    }

    partial void OnCityChanged(string value)
    {
        if (_selectingCity) return;

        if (string.IsNullOrEmpty(value))
        {
            LimpaForm();
            return;
        }

        _selectedCity = null;
        ClearCitySuggestions();
        if (string.IsNullOrWhiteSpace(value))
            CancelLoad();
        else if (value.Trim().Length >= 2)
            _ = LoadCitySuggestionsAsync(value);
    }

    private (string City, CitySuggestion? SelectedCity) GetForm() =>
        (City?.Trim() ?? "", _selectedCity);

    private void SetForm(CitySuggestion? city)
    {
        ClearCitySuggestions();
        _selectedCity = city;
        _selectingCity = true;
        try { City = city?.Name ?? ""; }
        finally { _selectingCity = false; }
    }

    private bool ValidaForm(string city)
    {
        if (!string.IsNullOrWhiteSpace(city)) return true;

        ErrorMessage = "Informe uma cidade para consultar o clima.";
        return false;
    }

    private void LimpaForm()
    {
        CancelLoad();
        SetForm(null);
        ErrorMessage = null;
    }

    private void CancelLoad()
    {
        var previous = _loadCts;
        _loadCts = null;
        previous?.Cancel();
        IsBusy = false;
    }

    private void ClearCitySuggestions()
    {
        _citySearchCts?.Cancel();
        CitySuggestions.Clear();
        ShowCitySuggestions = false;
    }

    private async Task LoadCitySuggestionsAsync(string query)
    {
        using var cts = new CancellationTokenSource();
        _citySearchCts = cts;

        try
        {
            await Task.Delay(350, cts.Token);
            var matches = await _weather.SearchCitiesAsync(query, cts.Token);
            if (cts.IsCancellationRequested || !string.Equals(query, City, StringComparison.Ordinal)) return;

            foreach (var city in matches) CitySuggestions.Add(city);
            ShowCitySuggestions = CitySuggestions.Count > 0;
        }
        catch (OperationCanceledException) { }
        catch (ApiException) { }
        finally
        {
            if (ReferenceEquals(_citySearchCts, cts)) _citySearchCts = null;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SelectCityAsync(CitySuggestion? city)
    {
        if (city is null) return Task.CompletedTask;

        SetForm(city);
        return LoadAsync();
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task LoadAsync()
    {
        ClearCitySuggestions();
        var (city, selectedCity) = GetForm();
        if (!ValidaForm(city))
        {
            CancelLoad();
            return;
        }

        using var cts = new CancellationTokenSource();
        var previous = _loadCts;
        _loadCts = cts;
        previous?.Cancel();

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = selectedCity is null
                ? await _recommendations.GetAsync(city, cts.Token)
                : await _recommendations.GetAsync(selectedCity, cts.Token);
            if (cts.IsCancellationRequested) return;

            _displayedWeather = result.Weather;
            TemperatureText = $"{Math.Round(result.Weather.TemperatureC)}°";
            WeatherSummary = result.Weather.Description;
            CountryFlagUrl = _extras.GetCountryFlagUrl(CurrentCountryCode);
            State.Current = result.Atmosphere;

            Recommendations.Clear();
            foreach (var perfume in result.Perfumes) Recommendations.Add(perfume);
            OnPropertyChanged(nameof(DisplayCity));
            OnPropertyChanged(nameof(DisplayCountry));
            OnPropertyChanged(nameof(AtmosphereTitle));
            OnPropertyChanged(nameof(HasWeather));
            OnPropertyChanged(nameof(LoadingText));
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
        catch (ApiException ex)
        {
            if (!cts.IsCancellationRequested) ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            if (!cts.IsCancellationRequested) ErrorMessage = "Não foi possível atualizar o clima e a curadoria.";
        }
        finally
        {
            if (ReferenceEquals(_loadCts, cts))
            {
                _loadCts = null;
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    private Task OpenPerfumeAsync(PerfumeSummary? perfume) =>
        perfume is null ? Task.CompletedTask : Navigation.OpenDetailAsync(perfume);
}
