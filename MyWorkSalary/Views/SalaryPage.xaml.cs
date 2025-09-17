using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Views
{
    public partial class SalaryPage : ContentPage
    {
        public SalaryPage(SalaryPageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

            // Lyssna på ändring av IsObExpanded
            if (BindingContext is SalaryPageViewModel vm)
            {
                vm.PropertyChanged += Vm_PropertyChanged;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is SalaryPageViewModel vm)
            {
                await vm.LoadData(); // Hämtar ActiveJob + beräknar CurrentStats
            }
        }

        private async void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SalaryPageViewModel.IsObExpanded))
            {
                if (BindingContext is SalaryPageViewModel vm)
                {
                    if (vm.IsObExpanded)
                    {
                        ObDetailsPanel.IsVisible = true;
                        ObDetailsPanel.Opacity = 0;
                        await ObDetailsPanel.FadeTo(1, 250); // fade in
                    }
                    else
                    {
                        await ObDetailsPanel.FadeTo(0, 200); // fade out
                        ObDetailsPanel.IsVisible = false;
                    }
                }
            }
        }

    }
}
