using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Views
{
    public partial class SalaryPage : ContentPage
    {
        public SalaryPage(SalaryPageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is SalaryPageViewModel vm)
            {
                await vm.LoadData(); // Hämtar ActiveJob + beräknar CurrentStats
            }
        }


    }
}
