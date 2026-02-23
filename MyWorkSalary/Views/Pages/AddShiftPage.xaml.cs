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

            if (RegularShiftForm.BindingContext is RegularShiftViewModel shiftVm)
            {
                shiftVm.RefreshPremiumState();
                shiftVm.Reset();
            }

            NavigationHelper.UseNoAnimationBackButton(this);
        }
    }
}
