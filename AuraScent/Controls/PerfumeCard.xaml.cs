using System.Windows.Input;

namespace AuraScent.Controls;

public partial class PerfumeCard : ContentView
{
    public static readonly BindableProperty OpenCommandProperty = BindableProperty.Create(
        nameof(OpenCommand), typeof(ICommand), typeof(PerfumeCard));

    public static readonly BindableProperty IsListProperty = BindableProperty.Create(
        nameof(IsList), typeof(bool), typeof(PerfumeCard), false,
        propertyChanged: (view, _, _) => ((PerfumeCard)view).UpdateLayout());

    public ICommand? OpenCommand
    {
        get => (ICommand?)GetValue(OpenCommandProperty);
        set => SetValue(OpenCommandProperty, value);
    }

    public bool IsList
    {
        get => (bool)GetValue(IsListProperty);
        set => SetValue(IsListProperty, value);
    }

    public PerfumeCard()
    {
        InitializeComponent();
        Unloaded += (_, _) => CardSurface.ResetMotion();
    }

    private void UpdateLayout()
    {
        CardLayout.RowDefinitions.Clear();
        CardLayout.ColumnDefinitions.Clear();
        if (IsList)
        {
            CardLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(108) });
            CardLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            CardLayout.MinimumHeightRequest = 100;
        }
        else
        {
            CardLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(142) });
            CardLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            CardLayout.MinimumHeightRequest = 0;
        }

        Grid.SetRow(CardCopy, IsList ? 0 : 1);
        Grid.SetColumn(CardCopy, IsList ? 1 : 0);
    }

    private void OnPressed(object? sender, EventArgs e) => AnimatePress(0.97);
    private void OnReleased(object? sender, EventArgs e) => AnimatePress(1);

    private void AnimatePress(double scale)
    {
        if (Motion.IsEnabled) _ = CardSurface.ScaleToAsync(scale, 110, Easing.CubicOut);
    }
}
