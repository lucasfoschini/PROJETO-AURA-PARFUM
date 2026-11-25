using AuraScent.Services;
using System.ComponentModel;

namespace AuraScent;

public partial class App : Application
{
    private readonly AtmosphereState _atmosphere;

    public App(AtmosphereState atmosphere)
    {
        InitializeComponent();
        _atmosphere = atmosphere;
        ApplyAtmosphere();
        _atmosphere.PropertyChanged += OnAtmosphereChanged;
    }

    private void OnAtmosphereChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AtmosphereState.Current))
            MainThread.BeginInvokeOnMainThread(ApplyAtmosphere);
    }

    private void ApplyAtmosphere()
    {
        var atmosphere = _atmosphere.Current;
        Resources["Accent"] = atmosphere.Accent;
        Resources["AccentGlass"] = atmosphere.Accent.WithAlpha(0.13f);
        Resources["AtmosphereTop"] = atmosphere.Top;
        var mid = new Color(
            atmosphere.Bottom.Red + (atmosphere.Mid.Red - atmosphere.Bottom.Red) * 0.32f,
            atmosphere.Bottom.Green + (atmosphere.Mid.Green - atmosphere.Bottom.Green) * 0.32f,
            atmosphere.Bottom.Blue + (atmosphere.Mid.Blue - atmosphere.Bottom.Blue) * 0.32f);
        Resources["AtmosphereBackdrop"] = new LinearGradientBrush(
            new GradientStopCollection
            {
                new(atmosphere.Top, 0),
                new(mid, 0.42f),
                new(atmosphere.Bottom, 1)
            },
            new Point(0.05, 0), new Point(0.95, 1));
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new(new AppShell()) { Title = "Aura Parfum" };
}
