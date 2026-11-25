namespace AuraScent.Controls;

public static class Motion
{
    private const string RevealAnimation = "aura-reveal";

    public static bool IsEnabled
    {
        get
        {
#if WINDOWS
            return new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
#elif ANDROID
            return OperatingSystem.IsAndroidVersionAtLeast(26)
                && Android.Animation.ValueAnimator.AreAnimatorsEnabled();
#elif IOS || MACCATALYST
            return !UIKit.UIAccessibility.IsReduceMotionEnabled;
#else
            return false;
#endif
        }
    }

    public static void Reveal(this VisualElement view, uint delay = 0, double travel = 12)
    {
        if (view.Handler is null || !IsEnabled) return;

        const double startOpacity = 0.6;
        const uint duration = 280;
        view.AbortAnimation(RevealAnimation);
        view.Opacity = startOpacity;
        view.TranslationY = travel;

        var sequence = new Animation();
        sequence.Add((double)delay / (delay + duration), 1, new Animation(progress =>
        {
            view.Opacity = startOpacity + (1 - startOpacity) * progress;
            view.TranslationY = travel * (1 - progress);
        }, easing: Easing.CubicOut));
        sequence.Commit(view, RevealAnimation, length: delay + duration);
    }

    public static void ResetMotion(this VisualElement view)
    {
        view.AbortAnimation(RevealAnimation);
        view.CancelAnimations();
        view.Opacity = 1;
        view.TranslationY = 0;
        view.Scale = 1;
    }
}
