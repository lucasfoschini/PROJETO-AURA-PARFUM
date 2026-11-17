namespace AuraScent.Controls;

public static class Motion
{
    /// <summary>Entrada suave: sobe 28px e aparece.</summary>
    public static async Task RevealAsync(this VisualElement view, int delayMs = 0, uint duration = 750)
    {
        view.Opacity = 0;
        view.TranslationY = 28;
        if (delayMs > 0) await Task.Delay(delayMs);
        await Task.WhenAll(
            view.FadeTo(1, duration, Easing.CubicOut),
            view.TranslateTo(0, 0, duration, Easing.CubicOut));
    }
}
