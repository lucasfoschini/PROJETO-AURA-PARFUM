using System.Text.Json.Serialization;
using AuraScent.Atmospheres;

namespace AuraScent.Models;

/// <summary>Perfume como aparece em listas e buscas.</summary>
public sealed record PerfumeSummary(string Name, string House, string Family, string PageUrl, string? ImageUrl = null)
{
    [JsonIgnore] public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
    [JsonIgnore] public bool HasNoImage => !HasImage;

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

/// <summary>Uma camada da pirâmide olfativa (topo, coração, fundo).</summary>
public sealed record NoteLayer(string Name, IReadOnlyList<string> Notes, IReadOnlyList<string>? OriginalNotes = null)
{
    /// <summary>Notas em inglês, como a Scento devolve (usadas para buscar fotos na Wikipedia).</summary>
    public IReadOnlyList<string> NotesEn => OriginalNotes ?? Notes;

    public string NotesText => string.Join("  ·  ", Notes);
}

/// <summary>Perfume com a pirâmide completa.</summary>
public sealed record PerfumeDetail(PerfumeSummary Summary, string Lede, IReadOnlyList<NoteLayer> Layers, string? ScentoUrl, string? LedeEn = null);

/// <summary>Resultado do cruzamento clima x perfumaria.</summary>
public sealed record Recommendation(WeatherSnapshot Weather, Atmosphere Atmosphere, IReadOnlyList<PerfumeSummary> Perfumes);

public sealed record RitualStep(string Layer, string Note);

/// <summary>Transforma a pirâmide em uma sequência guiada para o modo imersivo.</summary>
public static class OlfactoryRitual
{
    public static IReadOnlyList<RitualStep> Build(PerfumeDetail detail)
    {
        var steps = detail.Layers
            .SelectMany(layer => layer.Notes.Select(note => new RitualStep(layer.Name, note)))
            .ToList();

        if (steps.Count == 0)
            steps.Add(new RitualStep("Família olfativa", detail.Summary.DisplayFamily));

        return steps;
    }
}
