using System.Text.Json;
using AuraScent.Atmospheres;
using AuraScent.Models;
using AuraScent.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AuraScent.Services;

/// <summary>Estado global observável: a atmosfera atual pinta todas as telas.</summary>
public sealed partial class AtmosphereState : ObservableObject
{
    [ObservableProperty] private Atmosphere _current = AtmosphereCatalog.Default;
}

public sealed class PreferencesFavoritesService : IFavoritesService
{
    private const string Key = "aura.favorites";

    public IReadOnlyList<PerfumeSummary> GetAll()
    {
        try
        {
            var json = Preferences.Default.Get(Key, "[]");
            return JsonSerializer.Deserialize<List<PerfumeSummary>>(json) ?? new();
        }
        catch (JsonException)
        {
            return new List<PerfumeSummary>();
        }
    }

    public bool IsFavorite(PerfumeSummary perfume) => GetAll().Any(p => p.Slug == perfume.Slug);

    public bool Toggle(PerfumeSummary perfume)
    {
        var list = GetAll().ToList();
        var existing = list.FindIndex(p => p.Slug == perfume.Slug);
        var nowFavorite = existing < 0;

        if (nowFavorite) list.Add(perfume); else list.RemoveAt(existing);

        Preferences.Default.Set(Key, JsonSerializer.Serialize(list));
        return nowFavorite;
    }
}

public sealed class ShellNavigationService : INavigationService
{
    public Task OpenDetailAsync(PerfumeSummary perfume) =>
        Shell.Current.GoToAsync(nameof(DetailPage), new ShellNavigationQueryParameters { { "Perfume", perfume } });

    public Task OpenRitualAsync(PerfumeDetail detail) =>
        Shell.Current.GoToAsync(nameof(ImmersivePage), new ShellNavigationQueryParameters { { "Detail", detail } });

    public Task CloseAsync() => Shell.Current.GoToAsync("..");
}
