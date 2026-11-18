using AuraScent.Models;

namespace AuraScent.Services;

/// <summary>Open-Meteo: geocodificação + clima atual. Gratuita e sem chave.</summary>
public sealed class OpenMeteoApiClient : ApiClientBase, IWeatherService
{
    protected override string ServiceName => "Open-Meteo";

    public OpenMeteoApiClient(HttpClient http) : base(http) { }

    public async Task<WeatherSnapshot> GetCurrentAsync(string city, CancellationToken ct = default)
    {
        var location = await GeocodeAsync(string.IsNullOrWhiteSpace(city) ? "São Paulo" : city.Trim(), ct);
        return await GetCurrentAsync(location, ct);
    }

    public Task<WeatherSnapshot> GetCurrentAsync(CitySuggestion city, CancellationToken ct = default) =>
        GetCurrentAsync((city.Name, city.Latitude, city.Longitude, city.CountryCode), ct);

    public async Task<IReadOnlyList<CitySuggestion>> SearchCitiesAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<CitySuggestion>();

        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query.Trim())}&count=8&language=pt&format=json";
        using var doc = await GetJsonAsync(url, ct);
        if (doc.RootElement.Child("results") is not { ValueKind: System.Text.Json.JsonValueKind.Array } results)
            return Array.Empty<CitySuggestion>();

        return results.EnumerateArray()
            .Select(item => new CitySuggestion(
                item.Str("name") ?? "",
                item.Str("admin1") ?? "",
                item.Str("country") ?? "",
                item.Dbl("latitude"),
                item.Dbl("longitude"),
                item.Str("country_code") ?? ""))
            .Where(city => !string.IsNullOrWhiteSpace(city.Name))
            .DistinctBy(city => $"{city.Name}|{city.Region}|{city.Country}", StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<WeatherSnapshot> GetCurrentAsync((string Name, double Lat, double Lon, string CountryCode) location, CancellationToken ct)
    {
        var (name, lat, lon, countryCode) = location;

        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat.ToString(inv)}&longitude={lon.ToString(inv)}" +
                  "&current=temperature_2m,relative_humidity_2m,weather_code,is_day&timezone=auto";

        using var doc = await GetJsonAsync(url, ct);
        var current = doc.RootElement.Child("current")
            ?? throw new ApiException(ServiceName, "Open-Meteo não devolveu o clima atual.");

        return new WeatherSnapshot(
            name,
            current.Dbl("temperature_2m"),
            (int)current.Dbl("weather_code"),
            current.Dbl("is_day", 1) >= 1,
            (int)current.Dbl("relative_humidity_2m"),
            countryCode);
    }

    private async Task<(string Name, double Lat, double Lon, string CountryCode)> GeocodeAsync(string city, CancellationToken ct)
    {
        var results = await SearchCitiesAsync(city, ct);
        if (results.Count > 0)
        {
            var first = results[0];
            return (first.Name, first.Latitude, first.Longitude, first.CountryCode);
        }

        throw new ApiException(ServiceName, "N\u00e3o encontrei a cidade \u201c" + city + "\u201d.");
    }
}
