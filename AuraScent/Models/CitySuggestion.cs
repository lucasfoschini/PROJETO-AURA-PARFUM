namespace AuraScent.Models;

public sealed record CitySuggestion(string Name, string Region, string Country, double Latitude, double Longitude, string CountryCode = "")
{
    public string SecondaryText => string.Join(" · ", new[] { Region, Country }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase));

    public string FlagUrl =>
        CountryCode is { Length: 2 } code && code.All(char.IsAsciiLetter)
            ? $"https://flagcdn.com/w80/{code.ToLowerInvariant()}.png"
            : string.Empty;
}
