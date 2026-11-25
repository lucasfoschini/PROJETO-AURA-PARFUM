using System.Collections.ObjectModel;
using AuraScent.Models;
using AuraScent.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AuraScent.ViewModels;

public sealed partial class NoteItemViewModel : ObservableObject
{
    public string OriginalName { get; }

    [ObservableProperty] private string _name;

    public NoteItemViewModel(string originalName, string translatedName)
    {
        OriginalName = originalName;
        _name = translatedName;
    }
}

public sealed record NoteLayerDisplay(string Name, IReadOnlyList<NoteItemViewModel> Notes)
{
    private int Stage => Name.ToLowerInvariant() switch
    {
        "notas de topo" => 0,
        "notas de coração" => 1,
        "notas de fundo" => 2,
        _ => 3
    };

    public string StageIcon => Stage switch { 0 => "◇", 1 => "✧", 2 => "◆", _ => "·" };

    public string StageDescription => Stage switch
    {
        0 => "A abertura da fragrância",
        1 => "O caráter da composição",
        2 => "O rastro que permanece na pele",
        _ => ""
    };
}

public partial class DetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IPerfumeService _perfumes;
    private readonly IFavoritesService _favorites;
    private readonly IApisExtrasService _extras;
    private PerfumeDetail? _detail;
    private string _enrichmentStatus = "";

    public ObservableCollection<NoteLayerDisplay> Layers { get; } = new();

    [ObservableProperty, NotifyPropertyChangedFor(nameof(PresentedFamily), nameof(ScentoUrl))]
    private PerfumeSummary? _summary;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(PresentedDescription), nameof(PresentedCharacter))]
    private string _lede = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(PresentedDescription), nameof(PresentedCharacter))]
    private string _translatedLede = "";
    [ObservableProperty] private string _countryFlagUrl = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(PresentedCharacter))]
    private string _enrichedCharacter = "";
    [ObservableProperty] private string _enrichedConcentration = "";
    [ObservableProperty] private string _enrichedGender = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(PresentedFamily))]
    private string _enrichedFamily = "";
    [ObservableProperty] private string _accordsText = "";
    [ObservableProperty] private string _perfumersText = "";
    [ObservableProperty] private string _seasonsText = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(LongevityDescription))] private string _longevityText = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ProjectionDescription))] private string _projectionText = "";
    [ObservableProperty] private string _tenMlUrl = "";
    [ObservableProperty] private bool _hasEnrichedData;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowNoLayers))] private bool _isEnriching;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(FavoriteLabel), nameof(FavoriteButtonText), nameof(FavoriteActionDescription))]
    private bool _isFavorite;

    public string FavoriteLabel => IsFavorite ? "Nos favoritos" : "Favoritar";
    public string FavoriteButtonText => $"{(IsFavorite ? '♥' : '♡')}  {FavoriteLabel}";
    public string FavoriteActionDescription => IsFavorite ? "Remover dos favoritos" : "Adicionar aos favoritos";
    public string PresentedFamily => string.IsNullOrWhiteSpace(EnrichedFamily) ? Summary?.Family ?? "" : EnrichedFamily;
    public string PresentedDescription => string.IsNullOrWhiteSpace(TranslatedLede) ? Lede : TranslatedLede;
    public string PresentedCharacter => string.Equals(EnrichedCharacter, PresentedDescription, StringComparison.OrdinalIgnoreCase)
        ? "" : EnrichedCharacter;
    public bool HasLayers => Layers.Count > 0;
    public bool ShowNoLayers => _detail is not null && !HasLayers && !IsBusy && !IsEnriching;
    public string ScentoUrl => WebLink(_detail?.ScentoUrl ?? Summary?.PageUrl);
    public string LongevityDescription => DescribeRating(LongevityText);
    public string ProjectionDescription => DescribeRating(ProjectionText);
    public string EnrichmentStatus
    {
        get => _enrichmentStatus;
        private set => SetProperty(ref _enrichmentStatus, value);
    }

    public DetailViewModel(IPerfumeService perfumes, IFavoritesService favorites, AtmosphereState state, INavigationService navigation, IApisExtrasService extras)
        : base(state, navigation)
    {
        _perfumes = perfumes;
        _favorites = favorites;
        _extras = extras;
        Layers.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasLayers));
            OnPropertyChanged(nameof(ShowNoLayers));
        };
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IsBusy)) OnPropertyChanged(nameof(ShowNoLayers));
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
            OnPropertyChanged(nameof(ScentoUrl));

            SetLayers(_detail.Layers);

            _ = EnrichAsync();
        });
    }

    private void SetLayers(IEnumerable<NoteLayer> rawLayers)
    {
        var displayLayers = rawLayers
            .Where(l => l.Notes.Count > 0)
            .Select(l => new NoteLayerDisplay(
                l.Name,
                l.Notes.Select(n => new NoteItemViewModel(n, NoteTranslator.Translate(n))).ToList()))
            .ToList();

        Layers.Clear();
        foreach (var layer in displayLayers) Layers.Add(layer);
    }

    private async Task EnrichAsync()
    {
        if (Summary is null) return;
        IsEnriching = true;
        EnrichmentStatus = "";

        try
        {
            var hadInitialLayers = Layers.Count > 0;
            var translateTask = TranslateLedeAsync();
            var enrichTask = LoadEnrichedDataAsync();

            if (hadInitialLayers)
            {
                await Task.WhenAll(translateTask, enrichTask, TranslateUnknownNotesAsync());
            }
            else
            {
                await Task.WhenAll(translateTask, enrichTask);

                if (Layers.Count > 0)
                    await TranslateUnknownNotesAsync();
            }
        }
        catch (Exception)
        {
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
            if (!string.Equals(translated, Lede, StringComparison.Ordinal))
                TranslatedLede = translated;
        }
        catch (Exception)
        {
        }
    }

    private async Task LoadEnrichedDataAsync()
    {
        if (Summary is null) return;

        try
        {
            var data = await _extras.GetEnrichedDataAsync(Summary.Name, Summary.House);
            if (string.IsNullOrEmpty(data.Name))
            {
                EnrichmentStatus = "A 10ML não retornou dados adicionais para este perfume.";
                return;
            }

            HasEnrichedData = true;

            EnrichedConcentration = data.Concentration ?? "";
            EnrichedGender = TranslateGender(data.Gender);
            EnrichedFamily = data.Family ?? "";
            TenMlUrl = WebLink(data.TenMlUrl);

            if (data.Accords.Count > 0)
                AccordsText = string.Join("  ·  ", data.Accords.Select(NoteTranslator.Translate));

            if (data.Perfumers.Count > 0)
                PerfumersText = string.Join(", ", data.Perfumers);

            if (data.Seasons.Count > 0)
                SeasonsText = string.Join("  ·  ", data.Seasons.Select(TranslateSeason));

            LongevityText = data.Longevity.HasValue ? FormatRating(data.Longevity.Value) : "";
            ProjectionText = data.Projection.HasValue ? FormatRating(data.Projection.Value) : "";

            if (Layers.Count == 0 && data.HasPyramid)
            {
                var extra = new List<NoteLayer>();
                if (data.TopNotes.Count > 0)
                    extra.Add(new NoteLayer("Notas de topo", data.TopNotes));
                if (data.HeartNotes.Count > 0)
                    extra.Add(new NoteLayer("Notas de coração", data.HeartNotes));
                if (data.BaseNotes.Count > 0)
                    extra.Add(new NoteLayer("Notas de fundo", data.BaseNotes));

                await MainThread.InvokeOnMainThreadAsync(() => SetLayers(extra));
            }

            if (!string.IsNullOrWhiteSpace(data.Character))
            {
                EnrichedCharacter = data.Character;
                EnrichedCharacter = await _extras.TranslateToPortugueseAsync(data.Character);
            }
        }
        catch (Exception)
        {
            if (!HasEnrichedData) EnrichmentStatus = "Não foi possível consultar os dados adicionais da 10ML.";
        }
    }

    private async Task TranslateUnknownNotesAsync()
    {
        var unknownGroups = Layers
            .SelectMany(l => l.Notes)
            .Where(n => NoteTranslator.TryTranslate(n.OriginalName) is null)
            .GroupBy(n => n.OriginalName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unknownGroups.Count == 0) return;

        try
        {
            using var semaphore = new SemaphoreSlim(4, 4);
            await Task.WhenAll(unknownGroups.Select(async group =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var pt = await _extras.TranslateToPortugueseAsync(group.Key);
                    if (!string.IsNullOrWhiteSpace(pt))
                    {
                        var formatted = NoteTranslator.Capitalize(pt.Trim());
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            foreach (var item in group)
                                item.Name = formatted;
                        });
                    }
                }
                catch (Exception) { }
                finally { semaphore.Release(); }
            }));
        }
        catch (Exception)
        {
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

    private static string FormatRating(int value)
    {
        var stars = Math.Clamp(value, 0, 5);
        return new string('★', stars) + new string('☆', 5 - stars);
    }

    private static string DescribeRating(string rating) => $"{rating.Count(c => c == '★')} de 5";

    private static string WebLink(string? link) =>
        Uri.TryCreate(link, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http" ? uri.AbsoluteUri : "";

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
    private async Task OpenScentoAsync()
    {
        if (ScentoUrl is { Length: > 0 } url) await Launcher.Default.OpenAsync(url);
    }

    [RelayCommand]
    private async Task OpenTenMlAsync()
    {
        if (!string.IsNullOrWhiteSpace(TenMlUrl)) await Launcher.Default.OpenAsync(TenMlUrl);
    }
}
