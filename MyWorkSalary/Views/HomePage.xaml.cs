using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Views
{
    public partial class HomePage : ContentPage
    {
        private readonly HomeViewModel _viewModel;
        private bool _isFirstLoad = true;

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
                // Ladda data säkert med delay
                await Task.Delay(100); // Kort delay för att säkerställa UI är redo

                await Task.Run(() =>
                {
                    Dispatcher.Dispatch(() =>
                    {
                        _viewModel.RefreshData();
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🚨 HomePage OnAppearing Error: {ex}");

                // Fallback - visa error message
                await DisplayAlert("Fel", "Kunde inte ladda data. Försök igen.", "OK");
            }
        }
    }
}
