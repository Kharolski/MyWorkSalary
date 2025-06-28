namespace MyWorkSalary.Views;

public partial class HomePage : ContentPage
{

    public HomePage()
    {
        InitializeComponent();
    }

    private async void OnSetupJobClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Info", "Här kommer vi skapa jobb-setup senare!", "OK");
    }
}
