using AuraScent.Atmospheres;
using Microsoft.Maui.Dispatching;
using System.Diagnostics;

namespace AuraScent.Controls;

public sealed class AuroraBackground : ContentView
{
    public static readonly BindableProperty AtmosphereProperty = BindableProperty.Create(
        nameof(Atmosphere), typeof(Atmosphere), typeof(AuroraBackground), null,
        propertyChanged: (view, _, value) => ((AuroraBackground)view).SetAtmosphere((Atmosphere?)value));

    private readonly GraphicsView _scene = new() { InputTransparent = true };
    private readonly GraphicsView _outgoing = new() { InputTransparent = true, IsVisible = false };
    private AtmosphereScene _drawing = new(AtmosphereCatalog.Default);
    private IDispatcherTimer? _timer;
    private long _startedAt;
    private long _transitionAt;
    private bool _motionRequested;

    public Atmosphere? Atmosphere
    {
        get => (Atmosphere?)GetValue(AtmosphereProperty);
        set => SetValue(AtmosphereProperty, value);
    }

    public AuroraBackground()
    {
        InputTransparent = true;
        _scene.Drawable = _drawing;
        Content = new Grid { IsClippedToBounds = true, Children = { _scene, _outgoing } };
    }

    public void SetMotionActive(bool active)
    {
        _motionRequested = active;
        if (active && Handler is not null && Motion.IsEnabled)
            StartMotion();
        else
            StopMotion();
    }

    private void StopMotion()
    {
        if (_timer is not null)
        {
            _timer.Stop();
            _timer.Tick -= OnTick;
            _timer = null;
        }
        EndTransition();
    }

    private void StartMotion()
    {
        if (_timer is not null) return;
        _startedAt = Stopwatch.GetTimestamp();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(33);
        _timer.Tick += OnTick;
        _timer.Start();
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        if (args.OldHandler is not null) StopMotion();
        base.OnHandlerChanging(args);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        SetMotionActive(_motionRequested);
    }

    private void SetAtmosphere(Atmosphere? atmosphere)
    {
        atmosphere ??= AtmosphereCatalog.Default;
        if (_drawing.Atmosphere.GetType() == atmosphere.GetType()) return;

        if (_timer is not null)
        {
            // Keep the dominant scene if a new result arrives mid-transition.
            if (!_outgoing.IsVisible || _outgoing.Opacity <= 0.5)
                _outgoing.Drawable = _drawing;

            _outgoing.Opacity = 1;
            _outgoing.IsVisible = true;
            _outgoing.Invalidate();
            _transitionAt = Stopwatch.GetTimestamp();
        }

        _drawing = new AtmosphereScene(atmosphere);
        _scene.Drawable = _drawing;
        _scene.Invalidate();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _drawing.Seconds = (float)Stopwatch.GetElapsedTime(_startedAt).TotalSeconds;
        _scene.Invalidate();

        if (!_outgoing.IsVisible) return;
        var progress = Stopwatch.GetElapsedTime(_transitionAt).TotalMilliseconds / 500;
        if (progress >= 1)
            EndTransition();
        else
            _outgoing.Opacity = 1 - Easing.SinInOut.Ease(progress);
    }

    private void EndTransition()
    {
        _outgoing.IsVisible = false;
        _outgoing.Drawable = null;
    }
}
