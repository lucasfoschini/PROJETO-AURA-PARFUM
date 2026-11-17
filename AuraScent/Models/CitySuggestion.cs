namespace AuraScent.Models;

public sealed record CitySuggestion(string Name, string Region, string Country, double Latitude, double Longitude)
{
    public string SecondaryText => string.Join(" · ", new[] { Region, Country }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase));
}
