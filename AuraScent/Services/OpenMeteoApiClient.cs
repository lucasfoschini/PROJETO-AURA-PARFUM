using AuraScent.Models;

namespace AuraScent.Services;

/// <summary>Open-Meteo: geocodificação + clima atual. Gratuita e sem chave.</summary>
public sealed class OpenMeteoApiClient : ApiClientBase, IWeatherService
{
    protected override string ServiceName => "Open-Meteo";

    public OpenMeteoApiClient(HttpClient http) : base(http) { }

    public async Task<WeatherSnapshot> GetCurrentAsync(string city, CancellationToken ct = default)
    {
        var (name, lat, lon) = await GeocodeAsync(string.IsNullOrWhiteSpace(city) ? "São Paulo" : city.Trim(), ct);

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
            (int)current.Dbl("relative_humidity_2m"));
    }

    private async Task<(string Name, double Lat, double Lon)> GeocodeAsync(string city, CancellationToken ct)
    {
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=pt&format=json";
        using var doc = await GetJsonAsync(url, ct);

        if (doc.RootElement.Child("results") is { ValueKind: System.Text.Json.JsonValueKind.Array } results
            && results.GetArrayLength() > 0)
        {
            var first = results[0];
            return (first.Str("name") ?? city, first.Dbl("latitude"), first.Dbl("longitude"));
        }

        throw new ApiException(ServiceName, $"Não encontrei a cidade “{city}”.");
    }
}
