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
        Console.WriteLine("Passaram 7 cenários climáticos e as verificações dos formulários de cidade e perfumes.");
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
        Equal(true, ReferenceEquals(chosen, weather.LastSelection));
        Equal("São Paulo", weather.LastTextQuery);
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

        var homonym = new CitySuggestion("Santiago", "Veraguas", "Panamá", 8.10, -80.98, "PA");
        weather.Selection = new TaskCompletionSource<WeatherSnapshot>();
        var pendingSelection = vm.SelectCityCommand.ExecuteAsync(homonym);
        Equal(true, ReferenceEquals(homonym, weather.LastSelection));
        Equal("Santiago", vm.City);
        vm.City = "";
        Equal(true, weather.SelectionToken.IsCancellationRequested);
        Equal(false, vm.IsBusy);
        Equal(false, vm.ShowCitySuggestions);
        weather.Selection.SetResult(new WeatherSnapshot("Santiago", 30, 0, true, 50, "PA") { Country = "Panamá" });
        await pendingSelection;
        Equal("Chile", vm.DisplayCountry);
        Equal("11°", vm.TemperatureText);
        Equal("https://flagcdn.com/w160/cl.png", vm.CountryFlagUrl);
        Equal("fria", state.Current.Name);
        Equal("tobacco", vm.Recommendations[0].Name);

        vm.City = "  São Luís  ";
        weather.TextRequest = new TaskCompletionSource<WeatherSnapshot>();
        var pendingText = vm.LoadCommand.ExecuteAsync(null);
        Equal("São Luís", weather.LastTextQuery);
        vm.City = "   ";
        await vm.LoadCommand.ExecuteAsync(null);
        Equal(true, weather.TextRequestToken.IsCancellationRequested);
        Equal("Informe uma cidade para consultar o clima.", vm.ErrorMessage);
        Equal("São Luís", weather.LastTextQuery);
        weather.TextRequest.SetResult(new WeatherSnapshot("São Luís", 30, 0, true, 50, "BR"));
        await pendingText;
        Equal("Santiago", vm.DisplayCity);
        Equal(false, vm.IsBusy);

        var suggestionsShown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        weather.SuggestedCity = chosen;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(vm.ShowCitySuggestions) && vm.ShowCitySuggestions)
                suggestionsShown.TrySetResult();
        };
        vm.City = "S";
        vm.City = "Sa";
        vm.City = "San";
        await suggestionsShown.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Equal(1, weather.SuggestionsCalls);
        Equal(chosen, vm.CitySuggestions[0]);
        vm.City = "";
        Equal(0, vm.CitySuggestions.Count);
        Equal(false, vm.ShowCitySuggestions);
        Equal(false, vm.HasError);
    }

    private static async Task CheckExploreAsync()
    {
        var perfumes = new PerfumeStub();
        var vm = new ExploreViewModel(perfumes, new AtmosphereState(), new NavigationStub());
        Equal(true, vm.ShowSearchHint);
        Equal(false, vm.HasSearched);
        await vm.SearchCommand.ExecuteAsync(null);
        Equal("Informe uma nota, casa ou perfume para pesquisar.", vm.ErrorMessage);
        Equal(0, perfumes.SearchCount);
        await vm.SearchCommand.ExecuteAsync("resultado vazio");
        Equal(true, vm.HasSearched);
        Equal(true, vm.ShowSearchHint);
        await vm.SearchCommand.ExecuteAsync("Oud");
        Equal("Oud", vm.ResultsQuery);
        Equal("oud", vm.Results[0].Name);
        Equal(false, vm.ShowSearchHint);

        vm.Query = "  Oud Wood  ";
        await vm.SearchCommand.ExecuteAsync(null);
        Equal("Oud Wood", vm.Query);
        Equal("Oud Wood", perfumes.LastTerm);
        await vm.SelectSuggestionCommand.ExecuteAsync(new PerfumeSuggestion("Casa", "Tom Ford", "", "Tom Ford"));
        Equal("Tom Ford", vm.ResultsQuery);
        Equal("Tom Ford", perfumes.LastTerm);
        await vm.SearchCommand.ExecuteAsync("  Baunilha  ");
        Equal("Baunilha", vm.Query);
        Equal("Baunilha", vm.ResultsQuery);
        Equal("vanilla", perfumes.LastTerm);
        await vm.RetrySearchCommand.ExecuteAsync(null);
        Equal("vanilla", perfumes.LastTerm);
        await Task.Delay(500);
        Equal(6, perfumes.SearchCount);
        Equal(false, vm.ShowSuggestions);

        var suggestionsShown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(vm.ShowSuggestions) && vm.ShowSuggestions)
                suggestionsShown.TrySetResult();
        };
        vm.Query = "Ro";
        vm.Query = "Rosa";
        await suggestionsShown.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Equal(7, perfumes.SearchCount);
        Equal("rose", perfumes.LastTerm);
        Equal("Rosa", vm.Suggestions[0].Query);
        Equal("Baunilha", vm.ResultsQuery);

        perfumes.DelayTerm = "Tom Ford";
        var first = vm.SearchCommand.ExecuteAsync("Tom Ford");
        var slowResult = perfumes.DelayedResult;
        var slowToken = perfumes.DelayedToken;
        await vm.SearchCommand.ExecuteAsync("Oud");
        Equal(true, slowToken.IsCancellationRequested);
        slowResult.SetResult([new("Antigo", "Tom Ford", "", "")]);
        await first;
        Equal("Oud", vm.ResultsQuery);
        Equal("oud", vm.Results[0].Name);

        perfumes.DelayedResult = new TaskCompletionSource<IReadOnlyList<PerfumeSummary>>();
        var pending = vm.SearchCommand.ExecuteAsync("Tom Ford");
        Equal(true, vm.IsBusy);
        vm.Query = "";
        Equal(true, perfumes.DelayedToken.IsCancellationRequested);
        perfumes.DelayedResult.SetResult([new("Antigo", "Tom Ford", "", "")]);
        await pending;
        Equal(false, vm.IsBusy);
        Equal(false, vm.HasSearched);
        Equal<string?>(null, vm.ResultsQuery);
        Equal(0, vm.Results.Count);
        Equal(0, vm.Suggestions.Count);
        Equal(false, vm.ShowSuggestions);
        Equal(true, vm.ShowSearchHint);
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Esperava {expected}, obtive {actual}.");
    }

    private sealed class WeatherStub : IWeatherService
    {
        public TaskCompletionSource<WeatherSnapshot> TextRequest { get; set; } = new();
        public TaskCompletionSource<WeatherSnapshot> Selection { get; set; } = new();
        public CancellationToken TextRequestToken { get; private set; }
        public CancellationToken SelectionToken { get; private set; }
        public string? LastTextQuery { get; private set; }
        public CitySuggestion? LastSelection { get; private set; }
        public CitySuggestion? SuggestedCity { get; set; }
        public int SuggestionsCalls { get; private set; }

        public Task<WeatherSnapshot> GetCurrentAsync(string city, CancellationToken ct = default)
        {
            LastTextQuery = city;
            TextRequestToken = ct;
            return TextRequest.Task;
        }

        public Task<WeatherSnapshot> GetCurrentAsync(CitySuggestion city, CancellationToken ct = default)
        {
            LastSelection = city;
            SelectionToken = ct;
            return Selection.Task;
        }

        public Task<IReadOnlyList<CitySuggestion>> SearchCitiesAsync(string query, CancellationToken ct = default)
        {
            SuggestionsCalls++;
            return Task.FromResult<IReadOnlyList<CitySuggestion>>(SuggestedCity is { } city ? [city] : []);
        }
    }

    private sealed class PerfumeStub : IPerfumeService
    {
        public int SearchCount { get; private set; }
        public string? LastTerm { get; private set; }
        public string? DelayTerm { get; set; }
        public CancellationToken DelayedToken { get; private set; }
        public TaskCompletionSource<IReadOnlyList<PerfumeSummary>> DelayedResult { get; set; } = new();

        public Task<IReadOnlyList<PerfumeSummary>> SearchAsync(string term, CancellationToken ct = default)
        {
            SearchCount++;
            LastTerm = term;
            if (term == DelayTerm)
            {
                DelayedToken = ct;
                return DelayedResult.Task;
            }

            return Task.FromResult<IReadOnlyList<PerfumeSummary>>(term == "resultado vazio" ? [] : [new(term, "Casa", "", "")]);
        }

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
