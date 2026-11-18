using AuraScent.Models;

namespace AuraScent.Atmospheres;

public abstract class Atmosphere
{
    public abstract string Name { get; }
    public abstract string Emoji { get; }
    public abstract string Tagline { get; }

    public abstract Color Top { get; }
    public abstract Color Mid { get; }
    public virtual Color Bottom => Color.FromArgb("#07070D");
    public abstract Color Accent { get; }
    public abstract Color Glow { get; }

    public abstract IReadOnlyList<string> SearchTerms { get; }

    public abstract bool Fits(WeatherSnapshot weather);
}

public sealed class NightAtmosphere : Atmosphere
{
    public override string Name => "noturna";
    public override string Emoji => "🌙";
    public override string Tagline => "A noite pede profundidade: âmbar, oud e baunilha.";
    public override Color Top => Color.FromArgb("#0A0420");
    public override Color Mid => Color.FromArgb("#2B1055");
    public override Color Accent => Color.FromArgb("#C7A6FF");
    public override Color Glow => Color.FromArgb("#6A2FC2");
    public override IReadOnlyList<string> SearchTerms { get; } = new[] { "oud", "noir", "vanilla" };
    public override bool Fits(WeatherSnapshot w) => !w.IsDay;
}

public sealed class RainyAtmosphere : Atmosphere
{
    public override string Name => "chuvosa";
    public override string Emoji => "🌧️";
    public override string Tagline => "Terra molhada, vetiver e madeiras úmidas.";
    public override Color Top => Color.FromArgb("#06141B");
    public override Color Mid => Color.FromArgb("#0F3A4A");
    public override Color Accent => Color.FromArgb("#5CE1E6");
    public override Color Glow => Color.FromArgb("#1B7F95");
    public override IReadOnlyList<string> SearchTerms { get; } = new[] { "vetiver", "wood", "bois" };
    public override bool Fits(WeatherSnapshot w) => w.IsRainy;
}

public sealed class ColdAtmosphere : Atmosphere
{
    public override string Name => "fria";
    public override string Emoji => "🧣";
    public override string Tagline => "Frio pede aconchego: tabaco, especiarias e âmbar.";
    public override Color Top => Color.FromArgb("#1A0B08");
    public override Color Mid => Color.FromArgb("#5A1F10");
    public override Color Accent => Color.FromArgb("#FFB068");
    public override Color Glow => Color.FromArgb("#C2501F");
    public override IReadOnlyList<string> SearchTerms { get; } = new[] { "tobacco", "amber", "spice" };
    public override bool Fits(WeatherSnapshot w) => w.TemperatureC <= 16;
}

public sealed class SunnyAtmosphere : Atmosphere
{
    public override string Name => "solar";
    public override string Emoji => "☀️";
    public override string Tagline => "Calor e luz: cítricos, neroli e frescor aquático.";
    public override Color Top => Color.FromArgb("#1E0B36");
    public override Color Mid => Color.FromArgb("#8A2F5E");
    public override Color Accent => Color.FromArgb("#FFD27D");
    public override Color Glow => Color.FromArgb("#FF7A59");
    public override IReadOnlyList<string> SearchTerms { get; } = new[] { "citrus", "neroli", "eau" };
    public override bool Fits(WeatherSnapshot w) => w.TemperatureC >= 28 || (w.IsClear && w.TemperatureC >= 20);
}

public sealed class TemperateAtmosphere : Atmosphere
{
    public override string Name => "amena";
    public override string Emoji => "🍃";
    public override string Tagline => "Equilíbrio: rosa, jasmim e almíscar.";
    public override Color Top => Color.FromArgb("#0B1B16");
    public override Color Mid => Color.FromArgb("#1E4D3F");
    public override Color Accent => Color.FromArgb("#A8F0C6");
    public override Color Glow => Color.FromArgb("#2E8B6E");
    public override IReadOnlyList<string> SearchTerms { get; } = new[] { "rose", "musk", "jasmine" };
    public override bool Fits(WeatherSnapshot w) => true;
}

public static class AtmosphereCatalog
{
    private static readonly IReadOnlyList<Atmosphere> All = new Atmosphere[]
    {
        new NightAtmosphere(), new RainyAtmosphere(), new ColdAtmosphere(),
        new SunnyAtmosphere(), new TemperateAtmosphere()
    };

    public static Atmosphere Default { get; } = new TemperateAtmosphere();

    public static Atmosphere For(WeatherSnapshot weather) => All.First(a => a.Fits(weather));
}
