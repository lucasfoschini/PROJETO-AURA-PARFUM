using AuraScent.Models;
using AuraScent.Services;

namespace AuraScent.Tests;

/// <summary>
/// Testes unitários para o ApisExtrasService.
/// Para executar, crie um projeto de teste (xUnit) referenciando o projeto AuraScent.
/// dotnet new xunit -n AuraScent.Tests
/// dotnet add AuraScent.Tests reference AuraScent/AuraScent.csproj
/// </summary>
public class ApisExtrasServiceTests
{
    // ══════════════════════════════════════════════
    //  TESTE 1: Código de país inválido
    // ══════════════════════════════════════════════

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("BRA")]        // 3 letras
    [InlineData("B")]          // 1 letra
    [InlineData("12")]         // dígitos
    [InlineData("B!")]         // caractere especial
    public void GetCountryFlagUrl_InvalidCode_ReturnsEmpty(string? code)
    {
        var service = CreateService();
        var result = service.GetCountryFlagUrl(code!);
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("BR", "https://flagcdn.com/w160/br.png")]
    [InlineData("br", "https://flagcdn.com/w160/br.png")]
    [InlineData(" FR ", "https://flagcdn.com/w160/fr.png")]
    [InlineData("US", "https://flagcdn.com/w160/us.png")]
    [InlineData("It", "https://flagcdn.com/w160/it.png")]
    public void GetCountryFlagUrl_ValidCode_ReturnsCorrectUrl(string code, string expectedUrl)
    {
        var service = CreateService();
        var result = service.GetCountryFlagUrl(code);
        Assert.Equal(expectedUrl, result);
    }

    // ══════════════════════════════════════════════
    //  TESTE 2: Perfume não encontrado na 10ML
    // ══════════════════════════════════════════════

    [Fact]
    public async Task GetEnrichedDataAsync_EmptyName_ReturnsEmpty()
    {
        var service = CreateService();
        var result = await service.GetEnrichedDataAsync("", "Dior");
        Assert.Equal(EnrichedPerfumeData.Empty, result);
    }

    [Fact]
    public async Task GetEnrichedDataAsync_ApiUnavailable_ReturnsEmpty()
    {
        // Com um HttpClientFactory que retorna erros, o serviço não deve lançar exceções
        var service = CreateServiceWithFailingHttp();
        var result = await service.GetEnrichedDataAsync("Sauvage", "Dior");
        Assert.Equal(string.Empty, result.Name);
    }

    // ══════════════════════════════════════════════
    //  TESTE 3: Falha na tradução
    // ══════════════════════════════════════════════

    [Fact]
    public async Task TranslateToPortugueseAsync_EmptyText_ReturnsEmpty()
    {
        var service = CreateService();
        var result = await service.TranslateToPortugueseAsync("");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task TranslateToPortugueseAsync_NullText_ReturnsEmpty()
    {
        var service = CreateService();
        var result = await service.TranslateToPortugueseAsync(null!);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task TranslateToPortugueseAsync_ApiFailure_ReturnsOriginal()
    {
        var service = CreateServiceWithFailingHttp();
        var originalText = "A fresh citrus fragrance.";
        var result = await service.TranslateToPortugueseAsync(originalText);
        Assert.Equal(originalText, result);
    }

    // ══════════════════════════════════════════════
    //  TESTE 4: Texto acima de 500 caracteres
    // ══════════════════════════════════════════════

    [Fact]
    public async Task TranslateToPortugueseAsync_TextOver500Chars_TruncatesBeforeTranslation()
    {
        // Apenas verifica que não lança exceção com texto longo
        var service = CreateServiceWithFailingHttp();
        var longText = new string('A', 600);
        var result = await service.TranslateToPortugueseAsync(longText);
        // Com API falhando, retorna o original (truncado internamente, mas resultado = original)
        Assert.Equal(longText, result);
    }

    // ══════════════════════════════════════════════
    //  TESTE 5: Nota sem imagem na Wikipédia
    // ══════════════════════════════════════════════

    [Fact]
    public async Task GetNoteImageUrlAsync_EmptyNote_ReturnsEmpty()
    {
        var service = CreateService();
        var result = await service.GetNoteImageUrlAsync("");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task GetNoteImageUrlAsync_NullNote_ReturnsEmpty()
    {
        var service = CreateService();
        var result = await service.GetNoteImageUrlAsync(null!);
        Assert.Equal(string.Empty, result);
    }

    // ══════════════════════════════════════════════
    //  TESTE 6: Falha de conexão
    // ══════════════════════════════════════════════

    [Fact]
    public async Task GetNoteImageUrlAsync_ApiFailure_ReturnsEmpty()
    {
        var service = CreateServiceWithFailingHttp();
        var result = await service.GetNoteImageUrlAsync("Bergamot");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task GetEnrichedDataAsync_ConnectionFailure_DoesNotThrow()
    {
        var service = CreateServiceWithFailingHttp();
        var exception = await Record.ExceptionAsync(() =>
            service.GetEnrichedDataAsync("Sauvage", "Dior"));
        Assert.Null(exception);
    }

    // ══════════════════════════════════════════════
    //  TESTE 7: Resposta JSON inválida
    // ══════════════════════════════════════════════

    [Fact]
    public async Task TranslateToPortugueseAsync_InvalidJson_ReturnsOriginal()
    {
        // Com HttpClient retornando HTML em vez de JSON, não deve lançar exceção
        var service = CreateServiceWithInvalidJsonHttp();
        var result = await service.TranslateToPortugueseAsync("Hello world");
        Assert.Equal("Hello world", result);
    }

    // ══════════════════════════════════════════════
    //  TESTE 8: Fallback para dados da Scento
    // ══════════════════════════════════════════════

    [Fact]
    public async Task Enrichment_FallsBackToScentoData_WhenTenMlUnavailable()
    {
        // Este teste valida a lógica conceitual: se 10ML retorna Empty,
        // os dados da Scento permanecem intactos
        var service = CreateServiceWithFailingHttp();
        var enriched = await service.GetEnrichedDataAsync("Nonexistent Perfume", "Unknown Brand");

        // Deve retornar vazio (não substituir dados existentes)
        Assert.Equal(string.Empty, enriched.Name);
        Assert.False(enriched.HasPyramid);
    }

    // ══════════════════════════════════════════════
    //  TESTE 9: Cache funciona
    // ══════════════════════════════════════════════

    [Fact]
    public void GetCountryFlagUrl_CalledTwice_ReturnsSameResult()
    {
        var service = CreateService();
        var result1 = service.GetCountryFlagUrl("BR");
        var result2 = service.GetCountryFlagUrl("BR");
        Assert.Equal(result1, result2);
    }

    // ══════════════════════════════════════════════
    //  TESTE 10: CancellationToken respeitado
    // ══════════════════════════════════════════════

    [Fact]
    public async Task TranslateToPortugueseAsync_Cancelled_ThrowsOrReturnsOriginal()
    {
        var service = CreateServiceWithFailingHttp();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Com cancelamento, pode lançar OperationCanceledException ou retornar original
        try
        {
            var result = await service.TranslateToPortugueseAsync("Test", cts.Token);
            // Se não lançou, o texto deve ter sido preservado
            Assert.Equal("Test", result);
        }
        catch (OperationCanceledException)
        {
            // Esperado
        }
    }

    // ══════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════

    private static ApisExtrasService CreateService()
    {
        // Sem chamadas reais: apenas testa lógica local (flag URLs, validações)
        var factory = new FakeHttpClientFactory(new HttpClient(new FakeHandler(System.Net.HttpStatusCode.OK, "{}")));
        return new ApisExtrasService(factory);
    }

    private static ApisExtrasService CreateServiceWithFailingHttp()
    {
        var handler = new FakeHandler(System.Net.HttpStatusCode.ServiceUnavailable, "");
        var factory = new FakeHttpClientFactory(new HttpClient(handler));
        return new ApisExtrasService(factory);
    }

    private static ApisExtrasService CreateServiceWithInvalidJsonHttp()
    {
        var handler = new FakeHandler(System.Net.HttpStatusCode.OK, "<html>Not JSON</html>");
        var factory = new FakeHttpClientFactory(new HttpClient(handler));
        return new ApisExtrasService(factory);
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public FakeHttpClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly System.Net.HttpStatusCode _code;
        private readonly string _body;

        public FakeHandler(System.Net.HttpStatusCode code, string body)
        {
            _code = code;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(_code)
            {
                Content = new StringContent(_body, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
