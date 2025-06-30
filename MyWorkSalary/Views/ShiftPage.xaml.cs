using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Views
{
    public partial class ShiftPage : ContentPage
    {
        public ShiftPage(ShiftPageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Uppdatera data när sidan visas (t.ex. efter att ha lagt till nytt pass)
            if (BindingContext is ShiftPageViewModel viewModel)
            {
                viewModel.LoadData();
            }
        }
    }
}
