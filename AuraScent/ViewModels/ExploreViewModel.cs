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
    public IReadOnlyList<string> QuickTerms { get; } = new[] { "oud", "rose", "vanilla", "citrus", "amber", "tobacco", "musk", "wood" };

    [ObservableProperty] private string _query = "";

    public ExploreViewModel(IPerfumeService perfumes, AtmosphereState state, INavigationService navigation)
        : base(state, navigation) => _perfumes = perfumes;

    [RelayCommand]
    private async Task SearchAsync(string? term)
    {
        var q = string.IsNullOrWhiteSpace(term) ? Query : term;
        if (string.IsNullOrWhiteSpace(q)) return;
        Query = q;

        await RunSafeAsync(async () =>
        {
            var found = await _perfumes.SearchAsync(q.Trim());
            Results.Clear();
            foreach (var p in found) Results.Add(p);
        });
    }

    [RelayCommand]
    private Task OpenPerfumeAsync(PerfumeSummary? perfume) =>
        perfume is null ? Task.CompletedTask : Navigation.OpenDetailAsync(perfume);
}
