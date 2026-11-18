namespace AuraScent.Models;

/// <summary>Dados complementares obtidos da 10ML para enriquecer um perfume da Scento.</summary>
public sealed record EnrichedPerfumeData(
    string Name,
    string House,
    string? Character,
    string? Concentration,
    string? Gender,
    string? Family,
    IReadOnlyList<string> TopNotes,
    IReadOnlyList<string> HeartNotes,
    IReadOnlyList<string> BaseNotes,
    IReadOnlyList<string> Accords,
    IReadOnlyList<string> Seasons,
    IReadOnlyList<string> Occasions,
    int? Longevity,
    int? Projection,
    IReadOnlyList<string> Perfumers,
    string? TenMlUrl)
{
    public static EnrichedPerfumeData Empty { get; } = new(
        "", "", null, null, null, null,
        Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
        Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
        null, null, Array.Empty<string>(), null);

    public bool HasPyramid => TopNotes.Count > 0 || HeartNotes.Count > 0 || BaseNotes.Count > 0;
}

/// <summary>Uma nota olfativa com sua imagem da Wikipédia.</summary>
public sealed record NoteWithImage(string Name, string ImageUrl);
