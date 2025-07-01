using MyWorkSalary.ViewModels;


namespace MyWorkSalary.Views.Pages;

public partial class AddOBRatePage : ContentPage
{
    public AddOBRatePage(AddOBRateViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}