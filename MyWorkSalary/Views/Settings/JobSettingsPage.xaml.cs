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