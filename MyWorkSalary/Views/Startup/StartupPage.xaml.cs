using Microsoft.Maui.Controls;

namespace MyWorkSalary.Views.Startup;

public partial class StartupPage : ContentPage
{
    public StartupPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        await RunStartupAnimation();
    }

    private async Task RunStartupAnimation()
    {
        // Visa logo
        await Logo.FadeTo(1, 800);
        
        // Visa titel
        await TitleLabel.FadeTo(1, 600);
        
        // Visa laddningstext
        await LoadingText.FadeTo(1, 400);
        
        // Visa laddningsindikator
        await Loader.FadeTo(1, 400);
        
        // Vänta länge så du kan titta noga på designen
        await Task.Delay(3000);
        
        // Navigera till AppShell (som innehåller hemskärmen och alla tabs)
        Application.Current.MainPage = new AppShell();
    }
}
