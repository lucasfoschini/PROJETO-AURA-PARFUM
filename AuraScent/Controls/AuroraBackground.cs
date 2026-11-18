using AuraScent.Atmospheres;
using Microsoft.Maui.Controls.Shapes;

namespace AuraScent.Controls;

public class AuroraBackground : ContentView
{
    public static readonly BindableProperty AtmosphereProperty = BindableProperty.Create(
        nameof(Atmosphere), typeof(global::AuraScent.Atmospheres.Atmosphere), typeof(AuroraBackground), null,
        propertyChanged: (b, _, n) =>
        {
            if (b is AuroraBackground bg) _ = bg.TransitionAsync(n as global::AuraScent.Atmospheres.Atmosphere);
        });

    private readonly Grid _stage = new() { IsClippedToBounds = true };
    private readonly BoxView _sky = new();
    private readonly Ellipse[] _orbs = new Ellipse[3];
    private CancellationTokenSource? _cts;

    public global::AuraScent.Atmospheres.Atmosphere? Atmosphere
    {
        get => (global::AuraScent.Atmospheres.Atmosphere?)GetValue(AtmosphereProperty);
        set => SetValue(AtmosphereProperty, value);
    }

    public AuroraBackground()
    {
        InputTransparent = true;
        _stage.Add(_sky);

        for (var i = 0; i < _orbs.Length; i++)
        {
            var size = 340 + i * 90;
            _orbs[i] = new Ellipse
            {
                WidthRequest = size, HeightRequest = size,
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
                Opacity = 0.6
            };
            _stage.Add(_orbs[i]);
        }

        Content = _stage;
        Apply(AtmosphereCatalog.Default);
    }

    private void Apply(global::AuraScent.Atmospheres.Atmosphere? a)
    {
        a ??= AtmosphereCatalog.Default;

        _sky.Background = new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(a.Top, 0f),
                new GradientStop(a.Mid, 0.55f),
                new GradientStop(a.Bottom, 1f)
            },
            new Point(0.2, 0), new Point(0.8, 1));

        var colors = new[] { a.Accent, a.Glow, a.Mid };
        for (var i = 0; i < _orbs.Length; i++)
        {
            _orbs[i].Fill = new RadialGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(colors[i].WithAlpha(0.85f), 0f),
                    new GradientStop(colors[i].WithAlpha(0f), 1f)
                },
                new Point(0.5, 0.5), 0.5);
        }
    }

    private async Task TransitionAsync(global::AuraScent.Atmospheres.Atmosphere? a)
    {
        if (Handler is null) { Apply(a); return; }
        await _stage.FadeTo(0.15, 250);
        Apply(a);
        await _stage.FadeTo(1, 700);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        _cts?.Cancel();
        _cts = null;
        foreach (var orb in _orbs) orb.CancelAnimations();

        if (Handler is null) return;

        _cts = new CancellationTokenSource();
        for (var i = 0; i < _orbs.Length; i++) _ = DriftAsync(_orbs[i], i + 7, _cts.Token);
    }

    private async Task DriftAsync(VisualElement orb, int seed, CancellationToken ct)
    {
        var rnd = new Random(seed * 31);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var w = Width > 0 ? Width : 400;
                var h = Height > 0 ? Height : 800;
                var x = (rnd.NextDouble() - 0.5) * w;
                var y = (rnd.NextDouble() - 0.5) * h * 0.9;
                var scale = 0.8 + rnd.NextDouble() * 0.7;
                var duration = (uint)rnd.Next(7000, 12000);

                await Task.WhenAll(
                    orb.TranslateTo(x, y, duration, Easing.SinInOut),
                    orb.ScaleTo(scale, duration, Easing.SinInOut));
            }
        }
        catch (Exception) {}
    }
}
