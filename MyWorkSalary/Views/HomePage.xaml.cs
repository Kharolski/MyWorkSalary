using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Views
{
    public partial class HomePage : ContentPage
    {
        private readonly HomeViewModel _viewModel;

        public HomePage(HomeViewModel viewModel)
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
                await _viewModel.RefreshDataAsync();
            }
            catch (Exception ex)
            {
                // Fallback - visa error message
                await DisplayAlert("Fel", "Kunde inte ladda data. Försök igen.", "OK");
            }
        }
    }
}
