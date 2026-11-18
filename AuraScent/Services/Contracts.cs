using AuraScent.Models;

namespace AuraScent.Services;

public interface IPerfumeService
{
    Task<IReadOnlyList<PerfumeSummary>> SearchAsync(string term, CancellationToken ct = default);
    Task<PerfumeDetail> GetDetailAsync(PerfumeSummary summary, CancellationToken ct = default);
}

public interface IWeatherService
{
    Task<WeatherSnapshot> GetCurrentAsync(string city, CancellationToken ct = default);
    Task<WeatherSnapshot> GetCurrentAsync(CitySuggestion city, CancellationToken ct = default);
    Task<IReadOnlyList<CitySuggestion>> SearchCitiesAsync(string query, CancellationToken ct = default);
}

public interface IFavoritesService
{
    IReadOnlyList<PerfumeSummary> GetAll();
    bool IsFavorite(PerfumeSummary perfume);
    bool Toggle(PerfumeSummary perfume);
}

public interface INavigationService
{
    Task OpenDetailAsync(PerfumeSummary perfume);
    Task CloseAsync();
}
