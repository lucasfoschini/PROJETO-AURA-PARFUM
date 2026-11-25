using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuraScent.Atmospheres;
using AuraScent.Models;
using AuraScent.Services;
using AuraScent.ViewModels;

namespace AuraScent.Tests;

internal static class AtmosphericRedesignChecks
{
    public static async Task Main()
    {
        CheckAtmospheres();
        await CheckCitySelectionAsync();
        await CheckExploreAsync();
        Console.WriteLine("Passaram 7 cenários climáticos, a troca de cidade com resposta fora de ordem e os estados de busca.");
    }

    private static void CheckAtmospheres()
    {
        (bool IsDay, int WeatherCode, double Celsius, string Expected)[] cases =
        [
            (false, 61, 10, "noturna"),
            (true, 61, 30, "chuvosa"),
            (true, 0, 16, "fria"),
            (true, 3, 28, "solar"),
            (true, 0, 20, "solar"),
            (true, 0, 19, "amena"),
            (true, 3, 24, "amena")
        ];

        foreach (var (isDay, code, celsius, expected) in cases)
            Equal(expected, AtmosphereCatalog.For(new WeatherSnapshot("Cidade", celsius, code, isDay, 60)).Name);
    }

    private static async Task CheckCitySelectionAsync()
    {
        var weather = new WeatherStub();
        var perfumes = new PerfumeStub();
        var state = new AtmosphereState();
        var vm = new HomeViewModel(new RecommendationService(weather, perfumes), weather, state, new NavigationStub(), new ExtrasStub());

        var first = vm.LoadCommand.ExecuteAsync(null);
        var chosen = new CitySuggestion("Santiago", "Región Metropolitana", "Chile", -33.45, -70.67, "CL");
        var second = vm.SelectCityCommand.ExecuteAsync(chosen);
        Equal(chosen, weather.LastSelection);
        Equal(true, weather.TextRequestToken.IsCancellationRequested);

        weather.Selection.SetResult(new WeatherSnapshot("Santiago", 11, 0, true, 70, "CL") { Country = "Chile" });
        await second;
        weather.TextRequest.SetResult(new WeatherSnapshot("São Paulo", 30, 0, true, 50, "BR") { Country = "Brasil" });
        await first;

        Equal("Santiago", vm.DisplayCity);
        Equal("Chile", vm.DisplayCountry);
        Equal("11°", vm.TemperatureText);
        Equal("Céu limpo", vm.WeatherSummary);
        Equal("CL", vm.CurrentCountryCode);
        Equal("https://flagcdn.com/w160/cl.png", vm.CountryFlagUrl);
        Equal("fria", state.Current.Name);
        Equal("tobacco", vm.Recommendations[0].Name);
        Equal(false, vm.IsBusy);

        weather.Selection = new TaskCompletionSource<WeatherSnapshot>();
        var retry = vm.LoadCommand.ExecuteAsync(null);
        Equal(true, vm.IsBusy);
        Equal("Santiago", vm.DisplayCity);
        weather.Selection.SetException(new ApiException("Open-Meteo", "Falha de teste"));
        await retry;
        Equal(true, vm.HasError);
        Equal("Santiago", vm.DisplayCity);
        Equal("fria", state.Current.Name);
        Equal("tobacco", vm.Recommendations[0].Name);
    }

    private static async Task CheckExploreAsync()
    {
        var vm = new ExploreViewModel(new PerfumeStub(), new AtmosphereState(), new NavigationStub());
        Equal(true, vm.ShowSearchHint);
        Equal(false, vm.HasSearched);
        await vm.SearchCommand.ExecuteAsync("resultado vazio");
        Equal(true, vm.HasSearched);
        Equal(true, vm.ShowSearchHint);
        await vm.SearchCommand.ExecuteAsync("Oud");
        Equal("Oud", vm.ResultsQuery);
        Equal("oud", vm.Results[0].Name);
        Equal(false, vm.ShowSearchHint);
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Esperava {expected}, obtive {actual}.");
    }

    private sealed class WeatherStub : IWeatherService
    {
        public TaskCompletionSource<WeatherSnapshot> TextRequest { get; } = new();
        public TaskCompletionSource<WeatherSnapshot> Selection { get; set; } = new();
        public CancellationToken TextRequestToken { get; private set; }
        public CitySuggestion? LastSelection { get; private set; }

        public Task<WeatherSnapshot> GetCurrentAsync(string city, CancellationToken ct = default)
        {
            TextRequestToken = ct;
            return TextRequest.Task;
        }

        public Task<WeatherSnapshot> GetCurrentAsync(CitySuggestion city, CancellationToken ct = default)
        {
            LastSelection = city;
            return Selection.Task;
        }

        public Task<IReadOnlyList<CitySuggestion>> SearchCitiesAsync(string query, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CitySuggestion>>([]);
    }

    private sealed class PerfumeStub : IPerfumeService
    {
        public Task<IReadOnlyList<PerfumeSummary>> SearchAsync(string term, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PerfumeSummary>>(term == "resultado vazio" ? [] : [new(term, "Casa", "", "")]);

        public Task<PerfumeDetail> GetDetailAsync(PerfumeSummary summary, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class NavigationStub : INavigationService
    {
        public Task OpenDetailAsync(PerfumeSummary perfume) => throw new NotSupportedException();
        public Task CloseAsync() => throw new NotSupportedException();
    }

    private sealed class ExtrasStub : IApisExtrasService
    {
        public string GetCountryFlagUrl(string code) => $"https://flagcdn.com/w160/{code.ToLowerInvariant()}.png";

        public Task<EnrichedPerfumeData> GetEnrichedDataAsync(string name, string house, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<string> TranslateToPortugueseAsync(string text, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
