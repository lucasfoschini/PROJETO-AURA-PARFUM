using AuraScent.Models;

namespace AuraScent.Services;

/// <summary>
/// Contrato para o serviço de APIs complementares: bandeiras, tradução, dados 10ML e imagens de notas.
/// </summary>
public interface IApisExtrasService
{
    /// <summary>Monta a URL da bandeira para um código ISO de 2 letras. Não faz requisição HTTP.</summary>
    string GetCountryFlagUrl(string countryCode);

    /// <summary>Consulta a 10ML para enriquecer os dados de um perfume encontrado pela Scento.</summary>
    Task<EnrichedPerfumeData> GetEnrichedDataAsync(string perfumeName, string house, CancellationToken ct = default);

    /// <summary>Traduz um texto do inglês para português brasileiro via MyMemory.</summary>
    Task<string> TranslateToPortugueseAsync(string text, CancellationToken ct = default);

    /// <summary>Obtém a URL de uma imagem da Wikipédia para uma nota olfativa.</summary>
    Task<string> GetNoteImageUrlAsync(string noteName, CancellationToken ct = default);
}
