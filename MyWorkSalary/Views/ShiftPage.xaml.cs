using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Views
{
    public partial class ShiftPage : ContentPage
    {
        private readonly ShiftPageViewModel _viewModel;

        public ShiftPage(ShiftPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                // Visa sidan direkt medan data laddas i bakgrunden
                await _viewModel.LoadDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🚨 ShiftPage OnAppearing Error: {ex}");
                System.Diagnostics.Debug.WriteLine($"🚨 Stack Trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"🚨 Inner Exception: {ex.InnerException}");

                // Fallback - visa error message
                await DisplayAlert("Fel", "Kunde inte ladda data. Försök igen.", "OK");
            }
        }
    }
}
