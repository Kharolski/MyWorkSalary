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

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Uppdatera data när sidan visas
            Dispatcher.Dispatch(() =>
            {
                _viewModel.RefreshData();
            });
        }
    }
}
