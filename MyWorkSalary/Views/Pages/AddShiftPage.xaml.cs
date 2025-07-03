using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Views.Pages
{
    public partial class AddShiftPage : ContentPage
    {
        public AddShiftPage(AddShiftViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

    }
}
