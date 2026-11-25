using System.Diagnostics;
using AuraScent.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AuraScent.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    protected BaseViewModel(AtmosphereState state, INavigationService navigation)
    {
        State = state;
        Navigation = navigation;
    }

    public AtmosphereState State { get; }
    protected INavigationService Navigation { get; }

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [RelayCommand]
    private Task ShowExploreAsync() => Shell.Current.GoToAsync(AppShell.ExploreRoute);

    [RelayCommand]
    private Task ShowFavoritesAsync() => Shell.Current.GoToAsync(AppShell.FavoritesRoute);

    protected async Task RunSafeAsync(Func<Task> work)
    {
        IsBusy = true;
        ErrorMessage = null;
        try { await work(); }
        catch (ApiException ex) { ErrorMessage = ex.Message; }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            ErrorMessage = "Algo saiu do previsto. Tente de novo.";
        }
        finally { IsBusy = false; }
    }
}
