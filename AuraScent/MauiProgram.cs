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
