using AuraScent.Models;
using AuraScent.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AuraScent.ViewModels;

public partial class ImmersiveViewModel : BaseViewModel, IQueryAttributable
{
    [ObservableProperty] private string _perfumeName = "";
    [ObservableProperty] private string _house = "";

    public IReadOnlyList<RitualStep> Steps { get; private set; } = Array.Empty<RitualStep>();

    public ImmersiveViewModel(AtmosphereState state, INavigationService navigation) : base(state, navigation) { }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Detail", out var value) && value is PerfumeDetail detail)
        {
            PerfumeName = detail.Summary.Name;
            House = detail.Summary.House;
            Steps = OlfactoryRitual.Build(detail);
        }
    }

    [RelayCommand] private Task CloseAsync() => Navigation.CloseAsync();
}
