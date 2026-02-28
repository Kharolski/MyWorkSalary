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

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                if (BindingContext is AddShiftViewModel viewModel)
                {
                    await viewModel.LoadDataAsync();
                }

                if (RegularShiftForm.BindingContext is RegularShiftViewModel shiftVm)
                {
                    shiftVm.RefreshPremiumState();
                    shiftVm.Reset();
                }
            }
            catch (Exception ex)
            {
                // Fallback - visa error message
                await DisplayAlert("Fel", "Kunde inte ladda skift-formulär. Försök igen.", "OK");
            }

            // Fixar ett känt MAUI/Shell-problem där sidan som navigeras tillbaka till
            NavigationHelper.UseNoAnimationBackButton(this);
        }
    }
}
