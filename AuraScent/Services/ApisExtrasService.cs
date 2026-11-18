using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using AuraScent.Models;

namespace AuraScent.Services;

public sealed class ApisExtrasService : IApisExtrasService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ConcurrentDictionary<string, string> _translationCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, EnrichedPerfumeData> _enrichedCache = new(StringComparer.OrdinalIgnoreCase);

    internal const string TenMlClientName = "10ML";
    internal const string MyMemoryClientName = "MyMemory";

    public ApisExtrasService(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public string GetCountryFlagUrl(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            return string.Empty;

        var code = countryCode.Trim().ToLowerInvariant();

        if (code.Length != 2 || !IsAsciiLettersOnly(code))
            return string.Empty;

        return $"https://flagcdn.com/w160/{code}.png";
    }

    private static bool IsAsciiLettersOnly(string s)
    {
        foreach (var c in s)
            if (c is < 'a' or > 'z') return false;
        return true;
    }

    public async Task<EnrichedPerfumeData> GetEnrichedDataAsync(string perfumeName, string house, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(perfumeName))
            return EnrichedPerfumeData.Empty;

        var cacheKey = $"{NormalizeForComparison(house)}|{NormalizeForComparison(perfumeName)}";
        if (_enrichedCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            var query = BuildSearchQuery(perfumeName, house);
            var http = _httpFactory.CreateClient(TenMlClientName);

            var url = $"perfumes?q={Uri.EscapeDataString(query)}&format=detailed&limit=5";
            using var response = await http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
                return CacheAndReturn(cacheKey, EnrichedPerfumeData.Empty);

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var result = FindBestMatch(doc.RootElement, perfumeName, house);
            return CacheAndReturn(cacheKey, result);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return CacheAndReturn(cacheKey, EnrichedPerfumeData.Empty);
        }
    }

    private static string BuildSearchQuery(string name, string house)
    {
        var cleanName = RemoveConcentrationSuffix(name);
        return string.IsNullOrWhiteSpace(house)
            ? cleanName
            : $"{house} {cleanName}";
    }

    private static string RemoveConcentrationSuffix(string name)
    {
        var lower = name.ToLowerInvariant();
        string[] suffixes = ["eau de parfum", "eau de toilette", "extrait de parfum",
                             "eau de cologne", "eau fraiche", " edp", " edt", " edc"];
        foreach (var suffix in suffixes)
        {
            var idx = lower.IndexOf(suffix, StringComparison.Ordinal);
            if (idx > 0)
                return name[..idx].Trim();
        }
        return name;
    }

    private static EnrichedPerfumeData FindBestMatch(JsonElement root, string targetName, string targetHouse)
    {
        if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            return EnrichedPerfumeData.Empty;

        var normalizedName = NormalizeForComparison(targetName);
        var normalizedHouse = NormalizeForComparison(targetHouse);

        EnrichedPerfumeData? bestMatch = null;
        double bestScore = 0;

        foreach (var item in results.EnumerateArray())
        {
            var itemName = item.Str("name") ?? "";
            var itemHouse = item.Str("house") ?? "";

            var score = CalculateMatchScore(normalizedName, normalizedHouse,
                NormalizeForComparison(itemName), NormalizeForComparison(itemHouse));

            if (score > bestScore && score >= 0.5)
            {
                bestScore = score;
                bestMatch = ParsePerfumeResult(item);
            }
        }

        return bestMatch ?? EnrichedPerfumeData.Empty;
    }

    private static double CalculateMatchScore(string targetName, string targetHouse, string candidateName, string candidateHouse)
    {
        double score = 0;

        if (!string.IsNullOrEmpty(targetHouse) && !string.IsNullOrEmpty(candidateHouse))
        {
            if (candidateHouse == targetHouse)
                score += 0.4;
            else if (candidateHouse.Contains(targetHouse) || targetHouse.Contains(candidateHouse))
                score += 0.25;
            else
                return 0;
        }

        var cleanTarget = RemoveConcentrationSuffix(targetName).Trim();
        var cleanCandidate = RemoveConcentrationSuffix(candidateName).Trim();

        if (cleanCandidate == cleanTarget)
            score += 0.6;
        else if (cleanCandidate.Contains(cleanTarget) || cleanTarget.Contains(cleanCandidate))
            score += 0.4;
        else
        {
            var targetWords = cleanTarget.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var candidateWords = new HashSet<string>(
                cleanCandidate.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase);

            if (targetWords.Length > 0)
            {
                var matchCount = targetWords.Count(w => candidateWords.Contains(w));
                var wordScore = (double)matchCount / targetWords.Length;
                score += wordScore * 0.4;
            }
        }

        return score;
    }

    private static EnrichedPerfumeData ParsePerfumeResult(JsonElement item)
    {
        var pyramid = item.Child("pyramid");
        var topNotes = ReadStringArray(pyramid?.Child("top"));
        var heartNotes = ReadStringArray(pyramid?.Child("heart"));
        var baseNotes = ReadStringArray(pyramid?.Child("base"));

        string? tenMlUrl = null;
        if (item.TryGetProperty("links", out var links) && links.ValueKind == JsonValueKind.Array)
        {
            foreach (var link in links.EnumerateArray())
            {
                if (link.Str("type") == "try")
                {
                    tenMlUrl = link.Str("url");
                    break;
                }
            }
        }

        return new EnrichedPerfumeData(
            Name: item.Str("name") ?? "",
            House: item.Str("house") ?? "",
            Character: item.Str("character"),
            Concentration: item.Str("concentration"),
            Gender: item.Str("gender"),
            Family: item.Str("family"),
            TopNotes: topNotes,
            HeartNotes: heartNotes,
            BaseNotes: baseNotes,
            Accords: ReadStringArray(item.Child("accords")),
            Seasons: ReadStringArray(item.Child("seasons")),
            Occasions: ReadStringArray(item.Child("occasions")),
            Longevity: ReadNullableInt(item, "longevity"),
            Projection: ReadNullableInt(item, "projection"),
            Perfumers: ReadStringArray(item.Child("perfumers")),
            TenMlUrl: tenMlUrl);
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement? element)
    {
        if (element is not { ValueKind: JsonValueKind.Array } array)
            return Array.Empty<string>();

        var list = new List<string>();
        foreach (var el in array.EnumerateArray())
        {
            var s = el.GetString();
            if (!string.IsNullOrWhiteSpace(s)) list.Add(s);
        }
        return list;
    }

    private static int? ReadNullableInt(JsonElement item, string property)
    {
        if (item.TryGetProperty(property, out var val) && val.ValueKind == JsonValueKind.Number)
            return val.GetInt32();
        return null;
    }

    private static string NormalizeForComparison(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(c));
        }

        var result = sb.ToString();
        result = string.Join(' ', result.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries));
        return result;
    }

    private EnrichedPerfumeData CacheAndReturn(string key, EnrichedPerfumeData data)
    {
        _enrichedCache.TryAdd(key, data);
        return data;
    }

    private const int MaxTranslationLength = 500;

    public async Task<string> TranslateToPortugueseAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? string.Empty;

        var trimmed = text.Trim();

        if (_translationCache.TryGetValue(trimmed, out var cached))
            return cached;

        var toTranslate = trimmed.Length > MaxTranslationLength
            ? trimmed[..MaxTranslationLength]
            : trimmed;

        try
        {
            var http = _httpFactory.CreateClient(MyMemoryClientName);
            var encodedText = Uri.EscapeDataString(toTranslate);
            var url = $"get?q={encodedText}&langpair=en|pt-BR";

            using var response = await http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
                return CacheTranslation(trimmed, trimmed);

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var translated = ExtractTranslation(doc.RootElement);

            if (string.IsNullOrWhiteSpace(translated))
                return CacheTranslation(trimmed, trimmed);

            var result = trimmed.Length > MaxTranslationLength
                ? translated + "…"
                : translated;

            return CacheTranslation(trimmed, result);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return CacheTranslation(trimmed, trimmed);
        }
    }

    private static string? ExtractTranslation(JsonElement root)
    {
        if (!root.TryGetProperty("responseStatus", out var status))
            return null;

        if (status.ValueKind == JsonValueKind.Number && status.GetInt32() != 200)
            return null;

        if (status.ValueKind == JsonValueKind.String &&
            status.GetString() != "200")
            return null;

        var translated = root.Child("responseData")?.Str("translatedText");
        return string.IsNullOrWhiteSpace(translated) ? null : translated;
    }

    private string CacheTranslation(string key, string value)
    {
        _translationCache.TryAdd(key, value);
        return value;
    }

}
