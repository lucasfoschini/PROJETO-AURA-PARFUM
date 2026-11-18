namespace AuraScent.Models;

/// <summary>Retrato imutável do clima atual de uma cidade.</summary>
public sealed record WeatherSnapshot(string City, double TemperatureC, int WeatherCode, bool IsDay, int HumidityPercent, string CountryCode = "")
{
    public bool IsRainy => WeatherCode is (>= 51 and <= 67) or (>= 80 and <= 82) or >= 95;
    public bool IsClear => WeatherCode is 0 or 1;

    public string Description => WeatherCode switch
    {
        0 => "Céu limpo",
        1 or 2 => "Poucas nuvens",
        3 => "Nublado",
        45 or 48 => "Neblina",
        >= 51 and <= 57 => "Garoa",
        >= 61 and <= 67 => "Chuva",
        >= 71 and <= 77 => "Neve",
        >= 80 and <= 82 => "Pancadas de chuva",
        >= 95 => "Tempestade",
        _ => "Tempo instável"
    };
}
