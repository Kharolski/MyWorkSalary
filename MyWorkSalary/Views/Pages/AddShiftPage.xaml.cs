using MyWorkSalary.Helpers;
using MyWorkSalary.ViewModels;
using MyWorkSalary.ViewModels.ShiftTypes;

namespace MyWorkSalary.Views.Pages
{
    public partial class AddShiftPage : ContentPage
    {
        public AddShiftPage(AddShiftViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is AddShiftViewModel vm)
                vm.OnPageAppearing();

            // Fixar ett känt MAUI/Shell-problem där sidan som navigeras tillbaka till
            NavigationHelper.UseNoAnimationBackButton(this);
        }
    }
}
