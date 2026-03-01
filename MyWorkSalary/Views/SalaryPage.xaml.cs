using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Views
{
    public partial class SalaryPage : ContentPage
    {
        private readonly SalaryPageViewModel _viewModel;

        public SalaryPage(SalaryPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Återställ alltid till nuvarande månad när sidan visas
            _viewModel.ResetToCurrentMonth();

            try
            {
                // Visa sidan direkt medan data laddas i bakgrunden
                await _viewModel.LoadDataAsync();
            }
            catch (Exception ex)
            {
                // Fallback - visa error message
                await DisplayAlert("Fel", "Kunde inte ladda data. Försök igen.", "OK");
            }
        }

        private void PrevMonthTapped(object sender, EventArgs e)
        {
            if (BindingContext is SalaryPageViewModel vm)
            {
                vm.PrevMonthCommand.Execute(null);
            }
        }

        private void NextMonthTapped(object sender, EventArgs e)
        {
            if (BindingContext is SalaryPageViewModel vm)
            {
                vm.NextMonthCommand.Execute(null);
            }
        }

        private void OnSwipedRight(object sender, SwipedEventArgs e)
        {
            if (BindingContext is SalaryPageViewModel vm)
            {
                vm.PrevMonthCommand.Execute(null);
            }
        }

        private void OnSwipedLeft(object sender, SwipedEventArgs e)
        {
            if (BindingContext is SalaryPageViewModel vm)
            {
                vm.NextMonthCommand.Execute(null);
            }
        }
    }
}
