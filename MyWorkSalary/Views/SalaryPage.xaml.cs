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

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is SalaryPageViewModel vm)
            {
                vm.LoadData();
            }
        }
    }
}
