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
}

public interface IFavoritesService
{
    IReadOnlyList<PerfumeSummary> GetAll();
    bool IsFavorite(PerfumeSummary perfume);
    /// <returns>true se ficou favorito, false se foi removido.</returns>
    bool Toggle(PerfumeSummary perfume);
}

public interface INavigationService
{
    Task OpenDetailAsync(PerfumeSummary perfume);
    Task OpenRitualAsync(PerfumeDetail detail);
    Task CloseAsync();
}
