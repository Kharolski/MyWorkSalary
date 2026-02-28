using MyWorkSalary.Helpers;
using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Views.Settings;

public partial class JobSettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public JobSettingsPage(SettingsViewModel viewModel)
        {
                InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Nollställ IsBusy för att säkerställa att skeleton visas
        _viewModel.IsBusy = false;  

        try
        {
            _viewModel.RefreshActiveJob();
            await _viewModel.LoadDataAsync();
        }
        catch (Exception ex)
        {
            // Fallback - visa error message
            await DisplayAlert("Fel", "Kunde inte ladda job-inställningar. Försök igen.", "OK");
        }

        // Fixar ett känt MAUI/Shell-problem där sidan som navigeras tillbaka till
        NavigationHelper.UseNoAnimationBackButton(this);
    }
}
