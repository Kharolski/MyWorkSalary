using MyWorkSalary.Helpers;
using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Views.Pages;

public partial class AddJobPage : ContentPage
{
    private readonly AddJobViewModel _viewModel;

    public AddJobPage(AddJobViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Rensa formuläret varje gång sidan visas
        _viewModel.ClearForm();

        // Fixar ett känt MAUI/Shell-problem där sidan som navigeras tillbaka till
        NavigationHelper.UseNoAnimationBackButton(this);
    }
}