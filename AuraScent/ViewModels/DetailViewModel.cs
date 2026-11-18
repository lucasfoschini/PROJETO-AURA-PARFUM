using System.Collections.ObjectModel;
using AuraScent.Models;
using AuraScent.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AuraScent.ViewModels;

public partial class DetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IPerfumeService _perfumes;
    private readonly IFavoritesService _favorites;
    private readonly IApisExtrasService _extras;
    private PerfumeDetail? _detail;

    public ObservableCollection<NoteLayer> Layers { get; } = new();
    public ObservableCollection<NoteWithImage> NoteImages { get; } = new();

    [ObservableProperty] private PerfumeSummary? _summary;
    [ObservableProperty] private string _lede = "";
    [ObservableProperty] private string _translatedLede = "";
    [ObservableProperty] private string _countryFlagUrl = "";
    [ObservableProperty] private string _enrichedCharacter = "";
    [ObservableProperty] private string _enrichedConcentration = "";
    [ObservableProperty] private string _enrichedGender = "";
    [ObservableProperty] private string _enrichedFamily = "";
    [ObservableProperty] private string _accordsText = "";
    [ObservableProperty] private string _perfumersText = "";
    [ObservableProperty] private string _seasonsText = "";
    [ObservableProperty] private string _occasionsText = "";
    [ObservableProperty] private string _longevityText = "";
    [ObservableProperty] private string _projectionText = "";
    [ObservableProperty] private string _tenMlUrl = "";
    [ObservableProperty] private bool _hasEnrichedData;
    [ObservableProperty] private bool _isEnriching;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(FavoriteLabel))]
    private bool _isFavorite;

    public string FavoriteLabel => IsFavorite ? "Nos favoritos" : "Favoritar";

    public DetailViewModel(IPerfumeService perfumes, IFavoritesService favorites, AtmosphereState state, INavigationService navigation, IApisExtrasService extras)
        : base(state, navigation)
    {
        _perfumes = perfumes;
        _favorites = favorites;
        _extras = extras;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Perfume", out var value) && value is PerfumeSummary perfume && perfume != Summary)
        {
            Summary = perfume;
            IsFavorite = _favorites.IsFavorite(perfume);
            LoadCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (Summary is null) return;
        await RunSafeAsync(async () =>
        {
            _detail = await _perfumes.GetDetailAsync(Summary);
            Lede = _detail.Lede;
            Layers.Clear();
            foreach (var layer in _detail.Layers) Layers.Add(layer);

            // Iniciar enriquecimento complementar em paralelo (não bloqueia a tela)
            _ = EnrichAsync();
        });
    }

    private async Task EnrichAsync()
    {
        if (Summary is null) return;
        IsEnriching = true;

        try
        {
            // Executar todas as consultas complementares em paralelo
            var translateTask = TranslateLedeAsync();
            var enrichTask = LoadEnrichedDataAsync();
            var noteImagesTask = LoadNoteImagesAsync();

            await Task.WhenAll(translateTask, enrichTask, noteImagesTask);
        }
        catch (Exception)
        {
            // Falhas do enriquecimento não devem afetar a tela
        }
        finally
        {
            IsEnriching = false;
        }
    }

    private async Task TranslateLedeAsync()
    {
        if (string.IsNullOrWhiteSpace(Lede)) return;

        try
        {
            var translated = await _extras.TranslateToPortugueseAsync(Lede);
            // Só atualiza se realmente traduziu (diferente do original)
            if (!string.Equals(translated, Lede, StringComparison.Ordinal))
                TranslatedLede = translated;
        }
        catch (Exception)
        {
            // Mantém o texto original
        }
    }

    private async Task LoadEnrichedDataAsync()
    {
        if (Summary is null) return;

        try
        {
            var data = await _extras.GetEnrichedDataAsync(Summary.Name, Summary.House);
            if (string.IsNullOrEmpty(data.Name)) return; // Não encontrou

            HasEnrichedData = true;

            if (!string.IsNullOrWhiteSpace(data.Character))
            {
                // Traduzir o "character" da 10ML
                var translatedChar = await _extras.TranslateToPortugueseAsync(data.Character);
                EnrichedCharacter = translatedChar;
            }

            EnrichedConcentration = data.Concentration ?? "";
            EnrichedGender = TranslateGender(data.Gender);
            EnrichedFamily = data.Family ?? "";
            TenMlUrl = data.TenMlUrl ?? "";

            if (data.Accords.Count > 0)
                AccordsText = string.Join("  ·  ", data.Accords);

            if (data.Perfumers.Count > 0)
                PerfumersText = string.Join(", ", data.Perfumers);

            if (data.Seasons.Count > 0)
                SeasonsText = string.Join("  ·  ", data.Seasons.Select(TranslateSeason));

            if (data.Occasions.Count > 0)
                OccasionsText = string.Join("  ·  ", data.Occasions.Select(TranslateOccasion));

            LongevityText = data.Longevity.HasValue ? FormatRating(data.Longevity.Value, "Longevidade") : "";
            ProjectionText = data.Projection.HasValue ? FormatRating(data.Projection.Value, "Projeção") : "";

            // Se a 10ML tem pirâmide mais detalhada e a Scento não tem, complementa
            if (Layers.Count == 0 && data.HasPyramid)
            {
                if (data.TopNotes.Count > 0)
                    Layers.Add(new NoteLayer("Notas de topo", data.TopNotes));
                if (data.HeartNotes.Count > 0)
                    Layers.Add(new NoteLayer("Notas de coração", data.HeartNotes));
                if (data.BaseNotes.Count > 0)
                    Layers.Add(new NoteLayer("Notas de fundo", data.BaseNotes));
            }
        }
        catch (Exception)
        {
            // Dados da Scento permanecem como estão
        }
    }

    private async Task LoadNoteImagesAsync()
    {
        // Coleta todas as notas de todas as camadas
        var allNotes = Layers.SelectMany(l => l.Notes).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (allNotes.Count == 0) return;

        try
        {
            // Consultar as imagens em paralelo (máximo 10 simultâneas)
            var semaphore = new SemaphoreSlim(10, 10);
            var tasks = allNotes.Select(async note =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var imageUrl = await _extras.GetNoteImageUrlAsync(note);
                    return new NoteWithImage(note, imageUrl);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);

            NoteImages.Clear();
            foreach (var item in results)
            {
                if (!string.IsNullOrEmpty(item.ImageUrl))
                    NoteImages.Add(item);
            }
        }
        catch (Exception)
        {
            // Sem imagens de notas não é erro crítico
        }
    }

    private static string TranslateGender(string? gender) => gender?.ToLowerInvariant() switch
    {
        "men" => "Masculino",
        "women" => "Feminino",
        "unisex" => "Unissex",
        _ => gender ?? ""
    };

    private static string TranslateSeason(string season) => season.ToLowerInvariant() switch
    {
        "spring" => "Primavera",
        "summer" => "Verão",
        "autumn" or "fall" => "Outono",
        "winter" => "Inverno",
        _ => season
    };

    private static string TranslateOccasion(string occasion) => occasion.ToLowerInvariant() switch
    {
        "evening" => "Noite",
        "date night" => "Encontro",
        "office" => "Escritório",
        "daily" or "everyday" => "Dia a dia",
        "sport" or "gym" => "Esporte",
        "casual" => "Casual",
        "formal" => "Formal",
        "outdoor" or "outdoors" => "Ar livre",
        _ => occasion
    };

    private static string FormatRating(int value, string label)
    {
        var stars = new string('★', Math.Min(value, 5)) + new string('☆', Math.Max(0, 5 - value));
        return $"{label}: {stars}";
    }

    // Carrega bandeira do país quando o snapshot do clima está disponível
    public void SetCountryFlag(string countryCode)
    {
        CountryFlagUrl = _extras.GetCountryFlagUrl(countryCode);
    }

    [RelayCommand] private Task BackAsync() => Navigation.CloseAsync();

    [RelayCommand]
    private void ToggleFavorite()
    {
        if (Summary is not null) IsFavorite = _favorites.Toggle(Summary);
    }

    [RelayCommand]
    private Task EnterRitualAsync()
    {
        if (Summary is null) return Task.CompletedTask;
        var detail = _detail ?? new PerfumeDetail(Summary, "", Array.Empty<NoteLayer>(), null);
        return Navigation.OpenRitualAsync(detail);
    }

    [RelayCommand]
    private async Task OpenScentoAsync()
    {
        var url = _detail?.ScentoUrl ?? Summary?.PageUrl;
        if (!string.IsNullOrWhiteSpace(url)) await Launcher.Default.OpenAsync(url);
    }

    [RelayCommand]
    private async Task OpenTenMlAsync()
    {
        if (!string.IsNullOrWhiteSpace(TenMlUrl)) await Launcher.Default.OpenAsync(TenMlUrl);
    }
}
