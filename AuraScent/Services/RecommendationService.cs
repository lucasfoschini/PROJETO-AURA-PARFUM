using AuraScent.Atmospheres;
using AuraScent.Models;

namespace AuraScent.Services;

public sealed class RecommendationService
{
    private readonly IWeatherService _weather;
    private readonly IPerfumeService _perfumes;

    public RecommendationService(IWeatherService weather, IPerfumeService perfumes)
    {
        _weather = weather;
        _perfumes = perfumes;
    }

    public Task<Recommendation> GetAsync(string city, CancellationToken ct = default) =>
        BuildRecommendationAsync(token => _weather.GetCurrentAsync(city, token), ct);

    public Task<Recommendation> GetAsync(CitySuggestion city, CancellationToken ct = default) =>
        BuildRecommendationAsync(token => _weather.GetCurrentAsync(city, token), ct);

    private async Task<Recommendation> BuildRecommendationAsync(
        Func<CancellationToken, Task<WeatherSnapshot>> getWeather,
        CancellationToken ct)
    {
        var weather = await getWeather(ct);
        var atmosphere = AtmosphereCatalog.For(weather);

        var searches = await Task.WhenAll(atmosphere.SearchTerms.Select(t => SafeSearchAsync(t, ct)));
        var perfumes = Interleave(searches).DistinctBy(p => p.Slug).Take(10).ToList();

        if (perfumes.Count == 0)
            throw new ApiException("Scento", "A Scento não encontrou perfumes para esta atmosfera agora.");

        return new Recommendation(weather, atmosphere, perfumes);
    }

    private async Task<IReadOnlyList<PerfumeSummary>> SafeSearchAsync(string term, CancellationToken ct)
    {
        try { return await _perfumes.SearchAsync(term, ct); }
        catch (ApiException) { return Array.Empty<PerfumeSummary>(); }
    }

    private static IEnumerable<PerfumeSummary> Interleave(IReadOnlyList<IReadOnlyList<PerfumeSummary>> lists)
    {
        var max = lists.Count == 0 ? 0 : lists.Max(l => Math.Min(l.Count, 4));
        for (var i = 0; i < max; i++)
            foreach (var list in lists)
                if (i < list.Count) yield return list[i];
    }
}
