using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Views.Pages;

public partial class AddMultipleShiftsPage : ContentPage
{
	public AddMultipleShiftsPage(AddMultipleShiftsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;

    }
}