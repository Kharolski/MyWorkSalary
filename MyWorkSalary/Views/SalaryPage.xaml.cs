using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Views
{
    public partial class SalaryPage : ContentPage
    {
        private readonly SalaryPageViewModel _viewModel;
        private bool _isInitialized = false;

        public SalaryPage(SalaryPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!_isInitialized)
            {
                _viewModel.ResetToCurrentMonth();
                _isInitialized = true;
            }

            try
            {
                // Visa sidan direkt medan data laddas i bakgrunden
                await _viewModel.LoadDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🚨 SalaryPage OnAppearing Error: {ex}");
                System.Diagnostics.Debug.WriteLine($"🚨 Stack Trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"🚨 Inner Exception: {ex.InnerException}");

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
