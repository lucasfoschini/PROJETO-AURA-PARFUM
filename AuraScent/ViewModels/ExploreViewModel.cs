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
    public IReadOnlyList<string> QuickTerms { get; } = new[]
    {
        "Oud", "Rosa", "Baunilha", "Cítricos", "Âmbar", "Tabaco", "Almíscar", "Madeira"
    };

    [ObservableProperty] private string _query = "";
    [ObservableProperty] private bool _showSuggestions;

    private CancellationTokenSource? _suggestionsCts;
    private CancellationTokenSource? _searchCts;
    private string _lastSearch = "";
    private string? _resultsQuery;
    private bool _suppressQuerySuggestions;

    public ExploreViewModel(IPerfumeService perfumes, AtmosphereState state, INavigationService navigation)
        : base(state, navigation)
    {
        _perfumes = perfumes;
        Results.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasNoResults));
            OnPropertyChanged(nameof(ShowSearchHint));
        };
    }

    public bool HasNoResults => Results.Count == 0;
    public bool ShowSearchHint => HasNoResults && !IsBusy && !HasError;
    public bool HasSearched => ResultsQuery is not null;
    public string? ResultsQuery
    {
        get => _resultsQuery;
        private set
        {
            if (SetProperty(ref _resultsQuery, value)) OnPropertyChanged(nameof(HasSearched));
        }
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(IsBusy) or nameof(HasError))
            OnPropertyChanged(nameof(ShowSearchHint));
    }

    partial void OnQueryChanged(string value)
    {
        if (_suppressQuerySuggestions) return;

        if (string.IsNullOrEmpty(value))
        {
            LimpaForm();
            return;
        }

        var query = GetForm();
        if (query.Length == 0) CancelSearch();
        _ = LoadSuggestionsAsync(query);
    }

    private string GetForm() => Query?.Trim() ?? "";

    private void SetForm(string? term)
    {
        ClearSuggestions();
        _suppressQuerySuggestions = true;
        try { Query = term?.Trim() ?? ""; }
        finally { _suppressQuerySuggestions = false; }
    }

    private bool ValidaForm(string query)
    {
        if (!string.IsNullOrWhiteSpace(query)) return true;

        ErrorMessage = "Informe uma nota, casa ou perfume para pesquisar.";
        return false;
    }

    private void LimpaForm()
    {
        SetForm(null);
        _lastSearch = "";
        ResultsQuery = null;
        Results.Clear();
        ErrorMessage = null;
        CancelSearch();
    }

    private void CancelSearch()
    {
        var previous = _searchCts;
        _searchCts = null;
        previous?.Cancel();
        IsBusy = false;
    }

    private void ClearSuggestions()
    {
        _suggestionsCts?.Cancel();
        Suggestions.Clear();
        ShowSuggestions = false;
    }

    private async Task LoadSuggestionsAsync(string trimmedQuery)
    {
        ClearSuggestions();
        if (trimmedQuery.Length < 2) return;

        var cts = new CancellationTokenSource();
        _suggestionsCts = cts;

        try
        {
            await Task.Delay(350, cts.Token);
            if (cts.IsCancellationRequested || !string.Equals(trimmedQuery, GetForm(), StringComparison.OrdinalIgnoreCase)) return;

            foreach (var note in QuickTerms
                         .Where(term => NoteTranslator.ContainsIgnoringDiacritics(term, trimmedQuery) ||
                                        NoteTranslator.ToEnglishSearchTerm(term).Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase))
                         .Take(3))
                Suggestions.Add(new PerfumeSuggestion("Nota", note, "Buscar perfumes com esta nota", note));

            try
            {
                var apiQuery = NoteTranslator.ToEnglishSearchTerm(trimmedQuery);
                var matches = await _perfumes.SearchAsync(apiQuery, cts.Token);
                if (cts.IsCancellationRequested || !string.Equals(trimmedQuery, GetForm(), StringComparison.OrdinalIgnoreCase)) return;

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
                        string.Join(" · ", new[] { perfume.House, perfume.Family }
                            .Where(value => !string.IsNullOrWhiteSpace(value))),
                        perfume.Name));
            }
            catch (ApiException) { }

            if (!cts.IsCancellationRequested)
                ShowSuggestions = Suggestions.Count > 0;
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (ReferenceEquals(_suggestionsCts, cts)) _suggestionsCts = null;
            cts.Dispose();
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task SearchAsync(string? term)
    {
        SetForm(string.IsNullOrWhiteSpace(term) ? Query : term);
        var q = GetForm();
        if (!ValidaForm(q))
        {
            CancelSearch();
            return;
        }

        using var cts = new CancellationTokenSource();
        var previous = _searchCts;
        _searchCts = cts;
        previous?.Cancel();
        _lastSearch = q;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var found = await _perfumes.SearchAsync(NoteTranslator.ToEnglishSearchTerm(q), cts.Token);
            if (cts.IsCancellationRequested) return;
            ResultsQuery = q;
            Results.Clear();
            foreach (var p in found) Results.Add(p);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
        catch (ApiException ex)
        {
            if (!cts.IsCancellationRequested) ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            if (!cts.IsCancellationRequested) ErrorMessage = "Não foi possível buscar perfumes. Tente novamente.";
        }
        finally
        {
            if (ReferenceEquals(_searchCts, cts))
            {
                _searchCts = null;
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    private Task RetrySearchAsync() => SearchAsync(_lastSearch);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SelectSuggestionAsync(PerfumeSuggestion? suggestion) =>
        suggestion is null ? Task.CompletedTask : SearchAsync(suggestion.Query);

    [RelayCommand]
    private Task OpenPerfumeAsync(PerfumeSummary? perfume) =>
        perfume is null ? Task.CompletedTask : Navigation.OpenDetailAsync(perfume);
}
