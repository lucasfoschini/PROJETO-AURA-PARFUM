using System.Text.Json.Serialization;
using AuraScent.Atmospheres;

namespace AuraScent.Models;

public sealed record PerfumeSummary(string Name, string House, string Family, string PageUrl)
{
    [JsonIgnore] public string Slug => BuildSlug();
    [JsonIgnore] public string Initial => string.IsNullOrWhiteSpace(Name) ? "·" : Name.Trim()[..1].ToUpperInvariant();
    [JsonIgnore] public string DisplayFamily => string.IsNullOrWhiteSpace(Family) ? "Fragrância" : Family;

    private string BuildSlug()
    {
        if (Uri.TryCreate(PageUrl, UriKind.Absolute, out var uri))
        {
            var last = uri.Segments.LastOrDefault()?.Trim('/');
            if (!string.IsNullOrWhiteSpace(last)) return last;
        }
        var raw = $"{House} {Name}".ToLowerInvariant();
        return string.Join('-', raw.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}

public sealed record NoteLayer(string Name, IReadOnlyList<string> Notes)
{
    public string NotesText => string.Join("  ·  ", Notes);
}

public sealed record PerfumeDetail(PerfumeSummary Summary, string Lede, IReadOnlyList<NoteLayer> Layers, string? ScentoUrl);

public sealed record Recommendation(WeatherSnapshot Weather, Atmosphere Atmosphere, IReadOnlyList<PerfumeSummary> Perfumes);
