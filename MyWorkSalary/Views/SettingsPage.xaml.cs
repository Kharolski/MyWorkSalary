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
        System.Diagnostics.Debug.WriteLine("SettingsPage OnAppearing");

        if (BindingContext is SettingsViewModel viewModel)
        {
            viewModel.RefreshActiveJob();
        }
    }
}