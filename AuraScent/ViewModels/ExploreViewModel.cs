using System.Collections.ObjectModel;
using AuraScent.Models;
using AuraScent.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AuraScent.ViewModels;

public partial class ExploreViewModel : BaseViewModel
{
    private readonly IPerfumeService _perfumes;

    public ObservableCollection<PerfumeSummary> Results { get; } = new();
    public ObservableCollection<PerfumeSuggestion> Suggestions { get; } = new();
    public IReadOnlyList<string> QuickTerms { get; } = new[] { "oud", "rose", "vanilla", "citrus", "amber", "tobacco", "musk", "wood" };

    [ObservableProperty] private string _query = "";
    [ObservableProperty] private bool _showSuggestions;

    private CancellationTokenSource? _suggestionsCts;
    private bool _suppressQuerySuggestions;

    public ExploreViewModel(IPerfumeService perfumes, AtmosphereState state, INavigationService navigation)
        : base(state, navigation) => _perfumes = perfumes;

    partial void OnQueryChanged(string value)
    {
        if (!_suppressQuerySuggestions) _ = LoadSuggestionsAsync(value);
    }

    private async Task LoadSuggestionsAsync(string query)
    {
        var trimmedQuery = query.Trim();
        _suggestionsCts?.Cancel();
        Suggestions.Clear();
        ShowSuggestions = false;
        if (trimmedQuery.Length < 2) return;

        var cts = new CancellationTokenSource();
        var previous = _suggestionsCts;
        _suggestionsCts = cts;
        previous?.Cancel();

        try
        {
            await Task.Delay(350, cts.Token);
            if (cts.IsCancellationRequested || !string.Equals(trimmedQuery, Query.Trim(), StringComparison.OrdinalIgnoreCase)) return;

            foreach (var note in QuickTerms
                         .Where(term => term.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase))
                         .Take(3))
                Suggestions.Add(new PerfumeSuggestion("Nota", note, "Buscar perfumes com esta nota", note));

            try
            {
                var matches = await _perfumes.SearchAsync(trimmedQuery, cts.Token);
                if (cts.IsCancellationRequested || !string.Equals(trimmedQuery, Query.Trim(), StringComparison.OrdinalIgnoreCase)) return;

                foreach (var house in matches
                             .Select(perfume => perfume.House)
                             .Where(house => !string.IsNullOrWhiteSpace(house))
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .Take(3))
                    Suggestions.Add(new PerfumeSuggestion("Casa", house, "Buscar perfumes desta casa", house));

                foreach (var perfume in matches.DistinctBy(perfume => perfume.Slug).Take(5))
                    Suggestions.Add(new PerfumeSuggestion(
                        "Perfume",
                        perfume.Name,
                        string.Join(" · ", new[] { perfume.House, perfume.DisplayFamily }
                            .Where(value => !string.IsNullOrWhiteSpace(value))),
                        perfume.Name));
            }
            catch (ApiException) { }

            ShowSuggestions = Suggestions.Count > 0;
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (ReferenceEquals(_suggestionsCts, cts)) _suggestionsCts = null;
            cts.Dispose();
        }
    }

    [RelayCommand]
    private async Task SearchAsync(string? term)
    {
        var q = string.IsNullOrWhiteSpace(term) ? Query : term;
        if (string.IsNullOrWhiteSpace(q)) return;
        _suggestionsCts?.Cancel();
        Suggestions.Clear();
        ShowSuggestions = false;
        _suppressQuerySuggestions = true;
        Query = q;
        _suppressQuerySuggestions = false;

        await RunSafeAsync(async () =>
        {
            var found = await _perfumes.SearchAsync(q.Trim());
            Results.Clear();
            foreach (var p in found) Results.Add(p);
        });
    }

    [RelayCommand]
    private Task SelectSuggestionAsync(PerfumeSuggestion? suggestion) =>
        suggestion is null ? Task.CompletedTask : SearchAsync(suggestion.Query);

    [RelayCommand]
    private Task OpenPerfumeAsync(PerfumeSummary? perfume) =>
        perfume is null ? Task.CompletedTask : Navigation.OpenDetailAsync(perfume);
}
