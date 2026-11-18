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

        // API 1: Scento Perfume (perfumes, notas, pirâmide olfativa) - fonte principal
        s.AddHttpClient<ScentoApiClient>(http =>
        {
            http.BaseAddress = new Uri(ScentoApiClient.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(15);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AuraScent/1.0 (.NET MAUI)");
        });

        // API 3: 10ML Fragrance Index (fotos dos frascos) - complemento opcional, sem chave
        s.AddHttpClient<ITenMlClient, TenMlApiClient>(http =>
        {
            http.BaseAddress = new Uri(TenMlApiClient.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(8);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AuraScent/1.0 (.NET MAUI)");
        });

        // Fachada que as telas usam: Scento + 10ML, com plano B
        s.AddTransient<IPerfumeService, PerfumeCatalogService>();

        // Extras: bandeira (FlagCDN), tradução (MyMemory) e fotos de ingredientes (Wikipedia)
        s.AddHttpClient<IApisExtrasService, ApisExtrasService>(http =>
        {
            http.Timeout = TimeSpan.FromSeconds(10);
            // A Wikipedia exige um User-Agent identificável, com contato. Troque o e-mail (sem acentos).
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AuraScent/1.0 (projeto academico FATEC; contato: SEU-EMAIL@exemplo.com)");
        });

        // API 2: Open-Meteo (clima + geocodificação, sem chave)
        s.AddHttpClient<IWeatherService, OpenMeteoApiClient>(http =>
        {
            http.Timeout = TimeSpan.FromSeconds(15);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AuraScent/1.0 (.NET MAUI)");
        });

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
