using System.Text.Json;
using AuraScent.Models;

namespace AuraScent.Services;

public sealed class ScentoApiClient : ApiClientBase, IPerfumeService
{
    public const string BaseUrl = "https://www.scento.com/api/perfume/v1/";

    protected override string ServiceName => "Scento";

    public ScentoApiClient(HttpClient http) : base(http) { }

    public async Task<IReadOnlyList<PerfumeSummary>> SearchAsync(string term, CancellationToken ct = default)
    {
        using var doc = await GetJsonAsync($"search?q={Uri.EscapeDataString(term)}&kind=perfume", ct);

        var matches = doc.RootElement.Child("detail")?.Child("matches");
        if (matches is not { ValueKind: JsonValueKind.Array } array)
            return Array.Empty<PerfumeSummary>();

        var list = new List<PerfumeSummary>();
        foreach (var item in array.EnumerateArray())
        {
            var name = item.Str("name");
            if (string.IsNullOrWhiteSpace(name)) continue;
            list.Add(new PerfumeSummary(name, item.Str("house") ?? "", item.Str("family") ?? "", item.Str("page") ?? ""));
        }
        return list;
    }

    public async Task<PerfumeDetail> GetDetailAsync(PerfumeSummary summary, CancellationToken ct = default)
    {
        using var doc = await GetJsonAsync($"perfumes/{Uri.EscapeDataString(summary.Slug)}", ct);
        var root = doc.RootElement;

        var lede = root.Str("lede") ?? "";
        var layers = ParsePyramid(root.Child("detail"));
        var url = root.Child("scento_page")?.Str("url") ?? summary.PageUrl;

        return new PerfumeDetail(summary, lede, layers, url);
    }

    private static IReadOnlyList<NoteLayer> ParsePyramid(JsonElement? detail)
    {
        var layers = new List<NoteLayer>();
        if (detail?.Child("pyramid") is not { } pyramid) return layers;

        if (pyramid.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in pyramid.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                    layers.Add(new NoteLayer(Localize(prop.Name), ReadNames(prop.Value)));
            }
        }
        else if (pyramid.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in pyramid.EnumerateArray())
            {
                var label = item.Str("layer") ?? item.Str("label") ?? item.Str("name") ?? "Notas";
                if (item.Child("notes") is { ValueKind: JsonValueKind.Array } notes)
                    layers.Add(new NoteLayer(Localize(label), ReadNames(notes)));
            }
        }

        return layers.Where(l => l.Notes.Count > 0).ToList();
    }

    private static List<string> ReadNames(JsonElement array)
    {
        var names = new List<string>();
        foreach (var el in array.EnumerateArray())
        {
            var name = el.ValueKind == JsonValueKind.String ? el.GetString() : el.Str("name") ?? el.Str("note");
            if (!string.IsNullOrWhiteSpace(name)) names.Add(name!);
        }
        return names;
    }

    private static string Localize(string raw)
    {
        var s = raw.ToLowerInvariant();
        if (s.Contains("top") || s.Contains("head") || s.Contains("open")) return "Notas de topo";
        if (s.Contains("heart") || s.Contains("middle") || s.Contains("mid")) return "Notas de coração";
        if (s.Contains("base") || s.Contains("bottom") || s.Contains("dry")) return "Notas de fundo";
        return char.ToUpperInvariant(raw[0]) + raw[1..];
    }
}
