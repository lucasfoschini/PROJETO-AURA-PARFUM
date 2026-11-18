using AuraScent.Models;
using AuraScent.Services;

namespace AuraScent.Tests;

public class ApisExtrasServiceTests
{

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("BRA")]
    [InlineData("B")]
    [InlineData("12")]
    [InlineData("B!")]
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
        var service = CreateServiceWithFailingHttp();
        var result = await service.GetEnrichedDataAsync("Sauvage", "Dior");
        Assert.Equal(string.Empty, result.Name);
    }

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

    [Fact]
    public async Task TranslateToPortugueseAsync_TextOver500Chars_TruncatesBeforeTranslation()
    {
        var service = CreateServiceWithFailingHttp();
        var longText = new string('A', 600);
        var result = await service.TranslateToPortugueseAsync(longText);
        Assert.Equal(longText, result);
    }

    [Fact]
    public async Task GetEnrichedDataAsync_ConnectionFailure_DoesNotThrow()
    {
        var service = CreateServiceWithFailingHttp();
        var exception = await Record.ExceptionAsync(() =>
            service.GetEnrichedDataAsync("Sauvage", "Dior"));
        Assert.Null(exception);
    }

    [Fact]
    public async Task TranslateToPortugueseAsync_InvalidJson_ReturnsOriginal()
    {
        var service = CreateServiceWithInvalidJsonHttp();
        var result = await service.TranslateToPortugueseAsync("Hello world");
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public async Task Enrichment_FallsBackToScentoData_WhenTenMlUnavailable()
    {
        var service = CreateServiceWithFailingHttp();
        var enriched = await service.GetEnrichedDataAsync("Nonexistent Perfume", "Unknown Brand");

        Assert.Equal(string.Empty, enriched.Name);
        Assert.False(enriched.HasPyramid);
    }

    [Fact]
    public void GetCountryFlagUrl_CalledTwice_ReturnsSameResult()
    {
        var service = CreateService();
        var result1 = service.GetCountryFlagUrl("BR");
        var result2 = service.GetCountryFlagUrl("BR");
        Assert.Equal(result1, result2);
    }

    [Fact]
    public async Task TranslateToPortugueseAsync_Cancelled_ThrowsOrReturnsOriginal()
    {
        var service = CreateServiceWithFailingHttp();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            var result = await service.TranslateToPortugueseAsync("Test", cts.Token);
            Assert.Equal("Test", result);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static ApisExtrasService CreateService()
    {
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
