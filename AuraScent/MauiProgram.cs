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

        s.AddHttpClient<IPerfumeService, ScentoApiClient>(http =>
        {
            http.BaseAddress = new Uri(ScentoApiClient.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(15);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AuraScent/1.0 (.NET MAUI)");
        });

        s.AddHttpClient<IWeatherService, OpenMeteoApiClient>(http =>
        {
            http.Timeout = TimeSpan.FromSeconds(15);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AuraScent/1.0 (.NET MAUI)");
        });

        s.AddHttpClient(ApisExtrasService.TenMlClientName, http =>
        {
            http.BaseAddress = new Uri("https://mcp.10ml.com/api/v1/");
            http.Timeout = TimeSpan.FromSeconds(12);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AuraScent/1.0 (.NET MAUI)");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        s.AddHttpClient(ApisExtrasService.MyMemoryClientName, http =>
        {
            http.BaseAddress = new Uri("https://api.mymemory.translated.net/");
            http.Timeout = TimeSpan.FromSeconds(10);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AuraScent/1.0 (.NET MAUI)");
        });

        s.AddSingleton<IApisExtrasService, ApisExtrasService>();

        s.AddSingleton<AtmosphereState>();
        s.AddSingleton<IFavoritesService, PreferencesFavoritesService>();
        s.AddSingleton<INavigationService, ShellNavigationService>();
        s.AddTransient<RecommendationService>();

        s.AddTransient<HomeViewModel>();      s.AddTransient<HomePage>();
        s.AddTransient<ExploreViewModel>();   s.AddTransient<ExplorePage>();
        s.AddTransient<FavoritesViewModel>(); s.AddTransient<FavoritesPage>();
        s.AddTransient<DetailViewModel>();    s.AddTransient<DetailPage>();
        s.AddTransient<AboutPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
