using AuraScent.Models;
using AuraScent.ViewModels;

namespace AuraScent.Views;

/// <summary>
/// Ritual olfativo: respiração guiada (4s inspira / 4s expira) enquanto cada
/// nota da pirâmide surge na tela, camada por camada.
/// </summary>
public partial class ImmersivePage : ContentPage
{
    private readonly ImmersiveViewModel _vm;
    private CancellationTokenSource? _cts;

    public ImmersivePage(ImmersiveViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _cts = new CancellationTokenSource();
        await RunRitualAsync(_cts.Token);
    }

    protected override void OnDisappearing()
    {
        _cts?.Cancel();
        Core.CancelAnimations();
        Halo.CancelAnimations();
        NoteLabel.CancelAnimations();
        base.OnDisappearing();
    }

    private async Task RunRitualAsync(CancellationToken ct)
    {
        var steps = _vm.Steps;
        if (steps.Count == 0) return;

        try
        {
            for (var i = 0; !ct.IsCancellationRequested; i++)
                await PlayStepAsync(steps[i % steps.Count], i % steps.Count + 1, steps.Count, ct);
        }
        catch (OperationCanceledException) { /* usuário saiu do ritual */ }
    }

    private async Task PlayStepAsync(RitualStep step, int index, int total, CancellationToken ct)
    {
        LayerLabel.Text = step.Layer;
        NoteLabel.Text = step.Note;
        ProgressLabel.Text = $"{index} / {total}";
        NoteLabel.Opacity = 0;
        NoteLabel.TranslationY = 24;
        Pulse();

        // Inspira
        BreathLabel.Text = "Inspire";
        await Task.WhenAll(
            Core.ScaleTo(1.55, 4000, Easing.SinInOut),
            Halo.ScaleTo(1.35, 4000, Easing.SinInOut),
            NoteLabel.FadeTo(1, 1400, Easing.CubicOut),
            NoteLabel.TranslateTo(0, 0, 1400, Easing.CubicOut));
        ct.ThrowIfCancellationRequested();

        // Expira
        BreathLabel.Text = "Expire";
        await Task.WhenAll(
            Core.ScaleTo(1, 3600, Easing.SinInOut),
            Halo.ScaleTo(1, 3600, Easing.SinInOut));
        ct.ThrowIfCancellationRequested();

        await NoteLabel.FadeTo(0, 400);
        ct.ThrowIfCancellationRequested();
    }

    private static void Pulse()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); }
        catch (Exception) { /* dispositivo sem háptico ou sem permissão */ }
    }
}
