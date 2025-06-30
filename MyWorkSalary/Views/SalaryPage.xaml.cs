using MyWorkSalary.Views.Pages;

namespace MyWorkSalary.Views
{
    public partial class SalaryPage : ContentPage
    {
        public SalaryPage()
        {
            InitializeComponent();
        }

        // TEST METOD
        private async void OnTestAddShiftClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(AddShiftPage));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Navigation Fel", ex.Message, "OK");
            }
        }
    }
}
