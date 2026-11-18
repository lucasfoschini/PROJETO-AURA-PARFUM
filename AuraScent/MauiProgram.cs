using AuraScent.Atmospheres;
using AuraScent.Services;
using AuraScent.ViewModels;
using AuraScent.Views;
using Microsoft.Extensions.Logging;

namespace AuraScent;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var s = builder.Services;

        // API 1: Scento Perfume (perfumes, notas, pirâmide olfativa)
        s.AddHttpClient<IPerfumeService, ScentoApiClient>(http =>
        {
            http.BaseAddress = new Uri(ScentoApiClient.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(15);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AuraScent/1.0 (.NET MAUI)");
        });

        // API 2: Open-Meteo (clima + geocodificação, sem chave)
        s.AddHttpClient<IWeatherService, OpenMeteoApiClient>(http =>
        {
            http.Timeout = TimeSpan.FromSeconds(15);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AuraScent/1.0 (.NET MAUI)");
        });

        // API 3: 10ML Fragrance Index (dados enriquecidos de perfumes, sem chave)
        s.AddHttpClient(ApisExtrasService.TenMlClientName, http =>
        {
            http.BaseAddress = new Uri("https://mcp.10ml.com/api/v1/");
            http.Timeout = TimeSpan.FromSeconds(12);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AuraScent/1.0 (.NET MAUI)");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        // API 4: MyMemory Translation (tradução gratuita inglês → português)
        s.AddHttpClient(ApisExtrasService.MyMemoryClientName, http =>
        {
            http.BaseAddress = new Uri("https://api.mymemory.translated.net/");
            http.Timeout = TimeSpan.FromSeconds(10);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AuraScent/1.0 (.NET MAUI)");
        });

        // API 5: Wikipedia (imagens das notas olfativas)
        s.AddHttpClient(ApisExtrasService.WikipediaClientName, http =>
        {
            http.BaseAddress = new Uri("https://en.wikipedia.org/");
            http.Timeout = TimeSpan.FromSeconds(10);
            // Wikipedia exige User-Agent descritivo
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AuraScent/1.0 (perfume-discovery-app; contact@aurascent.app)");
        });

        // Serviço das APIs complementares
        s.AddSingleton<IApisExtrasService, ApisExtrasService>();

        // Serviços de domínio / estado compartilhado
        s.AddSingleton<AtmosphereState>();
        s.AddSingleton<IFavoritesService, PreferencesFavoritesService>();
        s.AddSingleton<INavigationService, ShellNavigationService>();
        s.AddTransient<RecommendationService>();

        // ViewModels + Views
        s.AddTransient<HomeViewModel>();      s.AddTransient<HomePage>();
        s.AddTransient<ExploreViewModel>();   s.AddTransient<ExplorePage>();
        s.AddTransient<FavoritesViewModel>(); s.AddTransient<FavoritesPage>();
        s.AddTransient<DetailViewModel>();    s.AddTransient<DetailPage>();
        s.AddTransient<ImmersiveViewModel>(); s.AddTransient<ImmersivePage>();
        s.AddTransient<AboutPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
