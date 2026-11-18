using Microsoft.Maui.Controls.Shapes;

namespace AuraScent.Controls;

public class MenuButton : ContentView
{
    public MenuButton()
    {
        var bars = new VerticalStackLayout { Spacing = 5, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        foreach (var width in new double[] { 22, 15, 22 })
        {
            bars.Add(new BoxView
            {
                WidthRequest = width, HeightRequest = 2, CornerRadius = 1,
                Color = Colors.White, HorizontalOptions = LayoutOptions.Start
            });
        }

        Content = new Border
        {
            WidthRequest = 48, HeightRequest = 48,
            StrokeThickness = 1,
            Stroke = Color.FromArgb("#55E8D5A8"),
            BackgroundColor = Color.FromArgb("#14FFFFFF"),
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            Content = bars
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (Shell.Current is not null) Shell.Current.FlyoutIsPresented = true;
        };
        GestureRecognizers.Add(tap);
    }
}
