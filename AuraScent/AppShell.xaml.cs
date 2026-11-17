using AuraScent.Views;

namespace AuraScent;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // MODO 3: navegação em pilha. DetailPage = push; ImmersivePage = modal em tela cheia.
        Routing.RegisterRoute(nameof(DetailPage), typeof(DetailPage));
        Routing.RegisterRoute(nameof(ImmersivePage), typeof(ImmersivePage));
    }
}
