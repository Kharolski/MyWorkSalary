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

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is SettingsViewModel viewModel)
        {
            viewModel.RefreshActiveJob();
        }

        // Fixar ett känt MAUI/Shell-problem där sidan som navigeras tillbaka till
        NavigationHelper.UseNoAnimationBackButton(this);
    }
}