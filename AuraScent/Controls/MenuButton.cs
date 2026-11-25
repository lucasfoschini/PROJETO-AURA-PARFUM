namespace AuraScent.Controls;

public sealed class MenuButton : Button
{
    public MenuButton()
    {
        SetDynamicResource(StyleProperty, "PillButton");
        ImageSource = "aura_menu.png";
        Padding = 0;
        WidthRequest = 48;
        HeightRequest = 48;
        SemanticProperties.SetDescription(this, "Abrir menu de navegação");
        Clicked += (_, _) =>
        {
            if (Shell.Current is { } shell)
                shell.FlyoutIsPresented = true;
        };
    }
}
