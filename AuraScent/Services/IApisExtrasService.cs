using AuraScent.Models;

namespace AuraScent.Services;

public interface IApisExtrasService
{
    string GetCountryFlagUrl(string countryCode);

    Task<EnrichedPerfumeData> GetEnrichedDataAsync(string perfumeName, string house, CancellationToken ct = default);

    Task<string> TranslateToPortugueseAsync(string text, CancellationToken ct = default);

}
