using MyWorkSalary.Helpers;
using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is SettingsViewModel viewModel)
        {
            try
            {
                await viewModel.LoadDataAsync();
            }
            catch (Exception ex)
            {
                // Fallback - visa error message
                await DisplayAlert("Fel", "Kunde inte ladda inställningar. Försök igen.", "OK");
            }
        }

        // Fixar ett känt MAUI/Shell-problem där sidan som navigeras tillbaka till
        NavigationHelper.UseNoAnimationBackButton(this);
    }
}