using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuraScent.Models;

namespace AuraScent.Services;

/// <summary>Resultado de uma tradução. Translated = false significa "mantive o texto original".</summary>
public sealed record TranslationResult(string Text, bool Translated);

public interface IApisExtrasService
{
    /// <summary>1) Bandeira (FlagCDN) a partir do código ISO de 2 letras. Devolve "" se o código for inválido.</summary>
    string GetFlagUrl(string? isoCode, int width = 320);

    /// <summary>2) Foto do frasco (10ML) por igualação de marca + nome. Devolve "" se não houver.</summary>
    Task<string> GetBottleImageUrlAsync(PerfumeSummary perfume, CancellationToken ct = default);

    /// <summary>3) Tradução en -> pt-BR (MyMemory). Nunca lança exceção; em falha devolve o original.</summary>
    Task<TranslationResult> TranslateToPtAsync(string? englishText, CancellationToken ct = default);

    /// <summary>4) Foto do ingrediente (Wikipedia em inglês). Devolve "" se não achar.</summary>
    Task<string> GetIngredientImageUrlAsync(string englishNote, CancellationToken ct = default);

    /// <summary>4) Várias notas de uma vez (no máximo 3 pedidos simultâneos). Chave = nome em inglês.</summary>
    Task<IReadOnlyDictionary<string, string>> GetIngredientImagesAsync(IEnumerable<string> englishNotes, CancellationToken ct = default);
}

/// <summary>
/// Quatro recursos gratuitos e sem chave, ligados aos dados que o app já tem:
/// país (Open-Meteo) -> bandeira | perfume (Scento) -> foto (10ML) |
/// descrição (Scento) -> português (MyMemory) | nota (Scento) -> foto (Wikipedia).
/// Todos os métodos públicos têm try/catch e devolvem um valor seguro ("" ou o texto original):
/// uma API fora do ar nunca derruba a tela.
/// </summary>
public sealed class ApisExtrasService : ApiClientBase, IApisExtrasService
{
    private const string FlagCdnUrl = "https://flagcdn.com";
    private const string MyMemoryUrl = "https://api.mymemory.translated.net/get";
    private const string WikipediaUrl = "https://en.wikipedia.org/w/api.php";

    // MyMemory: no máximo 500 BYTES por pedido (acentos contam mais de 1 byte). Margem de segurança.
    private const int MaxBytesPerRequest = 480;
    // Textos muito longos ficam no original, para não gastar o limite diário (5.000 caracteres sem e-mail).
    private const int MaxChunks = 3;
    // Opcional: informar um e-mail aqui sobe o limite diário da MyMemory de 5.000 para 50.000 caracteres.
    private const string MyMemoryEmail = "";

    // Larguras que a FlagCDN oferece.
    private static readonly int[] FlagWidths = { 20, 40, 80, 160, 320, 640, 1280, 2560 };

    // Notas ambíguas na Wikipedia ("oud" é um instrumento, "bergamot" abre uma página de desambiguação...).
    // Ajuste/complete conforme as fotos erradas que aparecerem.
    private static readonly Dictionary<string, string> WikiAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["oud"] = "Agarwood", ["agarwood"] = "Agarwood", ["bergamot"] = "Bergamot orange",
        ["iris"] = "Orris root", ["orris"] = "Orris root", ["mandarin"] = "Mandarin orange",
        ["cassis"] = "Blackcurrant", ["musk"] = "Musk", ["tonka"] = "Tonka bean"
    };

    // Caches em memória: economizam o limite da MyMemory e deixam a apresentação rápida.
    private static readonly ConcurrentDictionary<string, string> TranslationCache = new();
    private static readonly ConcurrentDictionary<string, string> IngredientCache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Regex SentenceSplit = new(@"(?<=[.!?])\s+", RegexOptions.Compiled);

    private readonly ITenMlClient _tenMl;

    protected override string ServiceName => "Extras";

    public ApisExtrasService(HttpClient http, ITenMlClient tenMl) : base(http) => _tenMl = tenMl;

    // ───────────── 1) Bandeira (FlagCDN) ─────────────

    public string GetFlagUrl(string? isoCode, int width = 320)
    {
        var code = isoCode?.Trim();
        if (code is not { Length: 2 } || !code.All(char.IsAsciiLetter)) return "";

        var allowed = FlagWidths.OrderBy(w => Math.Abs(w - width)).First();
        return $"{FlagCdnUrl}/w{allowed}/{code.ToLowerInvariant()}.png";
    }

    // ───────────── 2) Foto do frasco (10ML) ─────────────

    public async Task<string> GetBottleImageUrlAsync(PerfumeSummary perfume, CancellationToken ct = default)
    {
        // Se o PerfumeCatalogService já preencheu a foto na busca, não repete a chamada.
        if (perfume.HasImage) return perfume.ImageUrl!;

        try
        {
            var results = await _tenMl.SearchAsync($"{perfume.House} {perfume.Name}".Trim(), ct);
            return PerfumeMatcher.FindBest(perfume, results)?.ImageUrl ?? "";
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            Log("10ML", ex);
            return ""; // plano B: a tela mostra o frasco desenhado (BottleView)
        }
    }

    // ───────────── 3) Tradução (MyMemory) ─────────────

    public async Task<TranslationResult> TranslateToPtAsync(string? englishText, CancellationToken ct = default)
    {
        var original = englishText?.Trim() ?? "";
        if (original.Length == 0) return new TranslationResult(original, false);
        if (TranslationCache.TryGetValue(original, out var cached)) return new TranslationResult(cached, true);

        try
        {
            var chunks = SplitIntoChunks(original);
            if (chunks.Count > MaxChunks) return new TranslationResult(original, false);

            var parts = new List<string>();
            foreach (var chunk in chunks)
            {
                var translated = await TranslateChunkAsync(chunk, ct);
                if (translated is null) return new TranslationResult(original, false); // falha ou limite diário
                parts.Add(translated);
            }

            var result = string.Join(" ", parts);
            TranslationCache[original] = result;
            return new TranslationResult(result, true);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            Log("MyMemory", ex);
            return new TranslationResult(original, false);
        }
    }

    private async Task<string?> TranslateChunkAsync(string chunk, CancellationToken ct)
    {
        var url = $"{MyMemoryUrl}?q={Uri.EscapeDataString(chunk)}&langpair={Uri.EscapeDataString("en|pt-BR")}";
        if (MyMemoryEmail.Length > 0) url += $"&de={Uri.EscapeDataString(MyMemoryEmail)}";

        using var doc = await GetJsonAsync(url, ct);
        var root = doc.RootElement;
        if (!StatusIsOk(root)) return null;

        var text = root.Child("responseData")?.Str("translatedText");
        if (string.IsNullOrWhiteSpace(text)) return null;

        // Quando o limite diário acaba, a MyMemory devolve um aviso no lugar da tradução.
        if (text.Contains("MYMEMORY WARNING", StringComparison.OrdinalIgnoreCase)) return null;

        return WebUtility.HtmlDecode(text);
    }

    // A MyMemory às vezes manda responseStatus como número (200) e às vezes como texto ("200").
    private static bool StatusIsOk(JsonElement root)
    {
        if (root.Child("responseStatus") is not { } status) return true;
        return status.ValueKind switch
        {
            JsonValueKind.Number => status.TryGetInt32(out var code) && code == 200,
            JsonValueKind.String => status.GetString() == "200",
            _ => false
        };
    }

    // Quebra em frases e agrupa até ~480 bytes por pedido.
    private static List<string> SplitIntoChunks(string text)
    {
        var chunks = new List<string>();
        var current = new StringBuilder();

        foreach (var sentence in SentenceSplit.Split(text))
        {
            foreach (var piece in BreakLongSentence(sentence))
            {
                if (current.Length > 0 && ByteCount(current.ToString()) + 1 + ByteCount(piece) > MaxBytesPerRequest)
                {
                    chunks.Add(current.ToString());
                    current.Clear();
                }
                if (current.Length > 0) current.Append(' ');
                current.Append(piece);
            }
        }

        if (current.Length > 0) chunks.Add(current.ToString());
        return chunks;
    }

    // Frase sozinha acima do limite: quebra por palavras.
    private static IEnumerable<string> BreakLongSentence(string sentence)
    {
        if (ByteCount(sentence) <= MaxBytesPerRequest)
        {
            yield return sentence;
            yield break;
        }

        var line = new StringBuilder();
        foreach (var word in sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && ByteCount(line.ToString()) + 1 + ByteCount(word) > MaxBytesPerRequest)
            {
                yield return line.ToString();
                line.Clear();
            }
            if (line.Length > 0) line.Append(' ');
            line.Append(word);
        }
        if (line.Length > 0) yield return line.ToString();
    }

    private static int ByteCount(string s) => Encoding.UTF8.GetByteCount(s);

    // ───────────── 4) Foto do ingrediente (Wikipedia) ─────────────

    public async Task<string> GetIngredientImageUrlAsync(string englishNote, CancellationToken ct = default)
    {
        var note = englishNote?.Trim() ?? "";
        if (note.Length == 0) return "";
        if (IngredientCache.TryGetValue(note, out var cached)) return cached;

        try
        {
            foreach (var candidate in WikipediaCandidates(note).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var url = await SearchWikipediaImageAsync(candidate, ct);
                if (url.Length == 0) continue;

                IngredientCache[note] = url;
                return url;
            }

            IngredientCache[note] = ""; // pesquisou sem erro e não há foto: não tenta de novo
            return "";
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            Log("Wikipedia", ex);
            return ""; // erro de rede não vai para o cache: tenta de novo na próxima vez
        }
    }

    public async Task<IReadOnlyDictionary<string, string>> GetIngredientImagesAsync(
        IEnumerable<string> englishNotes, CancellationToken ct = default)
    {
        var notes = englishNotes
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        using var gate = new SemaphoreSlim(3); // educado com a Wikipedia: poucos pedidos ao mesmo tempo
        var tasks = notes.Select(async note =>
        {
            await gate.WaitAsync(ct);
            try { return (Note: note, Url: await GetIngredientImageUrlAsync(note, ct)); }
            finally { gate.Release(); }
        });

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r.Note, r => r.Url, StringComparer.OrdinalIgnoreCase);
    }

    // Busca inteligente: apelido conhecido -> nome completo -> nome sem as primeiras palavras.
    // "Sicilian Lemon" tenta "Sicilian Lemon" e depois "Lemon".
    private static IEnumerable<string> WikipediaCandidates(string note)
    {
        if (WikiAliases.TryGetValue(note, out var alias)) yield return alias;
        yield return note;

        var words = note.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var skip = 1; skip < words.Length && skip <= 2; skip++)
            yield return string.Join(' ', words.Skip(skip));
    }

    private async Task<string> SearchWikipediaImageAsync(string term, CancellationToken ct)
    {
        var url = $"{WikipediaUrl}?action=query&format=json&formatversion=2&redirects=1" +
                  $"&generator=search&gsrsearch={Uri.EscapeDataString(term)}&gsrnamespace=0&gsrlimit=1" +
                  "&prop=pageimages&piprop=thumbnail&pithumbsize=300";

        using var doc = await GetJsonAsync(url, ct);
        if (doc.RootElement.Child("query")?.Child("pages") is not { ValueKind: JsonValueKind.Array } pages)
            return ""; // sem resultado

        foreach (var page in pages.EnumerateArray())
        {
            var source = page.Child("thumbnail")?.Str("source");
            if (!string.IsNullOrWhiteSpace(source) && source.StartsWith("https://", StringComparison.Ordinal))
                return source;
        }
        return "";
    }

    private static void Log(string service, Exception ex) =>
        Debug.WriteLine($"{service} indisponível, seguindo sem ele: {ex.Message}");
}
