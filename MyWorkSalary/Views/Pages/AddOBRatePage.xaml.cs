using MyWorkSalary.Helpers;
using MyWorkSalary.ViewModels;


namespace MyWorkSalary.Views.Pages
{
    [QueryProperty(nameof(OBRateId), "obRateId")]
    public partial class AddOBRatePage : ContentPage
    {
        public string OBRateId { get; set; } = "0";
        private readonly AddOBRateViewModel _viewModel;

        public AddOBRatePage(AddOBRateViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (int.TryParse(OBRateId, out var id) && id > 0)
            {
                _viewModel.LoadForEdit(id);
            }
            else
            {
                _viewModel.PrepareForCreate();
            }

            // Fixar ett känt MAUI/Shell-problem där sidan som navigeras tillbaka till
            NavigationHelper.UseNoAnimationBackButton(this);
        }
    }
}