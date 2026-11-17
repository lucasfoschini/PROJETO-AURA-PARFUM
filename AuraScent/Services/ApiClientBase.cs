using System.Text.Json;

namespace AuraScent.Services;

public sealed class ApiException : Exception
{
    public string Service { get; }
    public ApiException(string service, string message, Exception? inner = null) : base(message, inner)
        => Service = service;
}

/// <summary>
/// Classe base abstrata dos clientes de API: encapsula HttpClient, JSON e tratamento
/// de erros. Cada API concreta herda e só implementa o que é específico dela.
/// </summary>
public abstract class ApiClientBase
{
    protected HttpClient Http { get; }
    protected abstract string ServiceName { get; }

    protected ApiClientBase(HttpClient http) => Http = http;

    protected async Task<JsonDocument> GetJsonAsync(string url, CancellationToken ct = default)
    {
        try
        {
            using var response = await Http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(ServiceName, $"Não consegui falar com {ServiceName}. Verifique sua conexão.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ApiException(ServiceName, $"{ServiceName} demorou demais para responder.", ex);
        }
        catch (JsonException ex)
        {
            throw new ApiException(ServiceName, $"{ServiceName} devolveu uma resposta inesperada.", ex);
        }
    }
}

internal static class JsonElementExtensions
{
    public static JsonElement? Child(this JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) ? v : null;

    public static string? Str(this JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    public static double Dbl(this JsonElement e, string name, double fallback = 0) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetDouble()
            : fallback;
}
