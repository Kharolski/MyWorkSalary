using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Views.Pages
{
    [QueryProperty(nameof(JobId), "jobId")]
    public partial class EditJobPage : ContentPage
    {
        private readonly EditJobViewModel _viewModel;
        private int _jobId;

        public string JobId
        {
            set => _jobId = int.Parse(value); // Bara spara värdet
        }

        public EditJobPage(EditJobViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Nu är allt klart - ladda jobbet
            if (_jobId > 0)
            {
                _viewModel.LoadJob(_jobId);
            }
        }
    }
}
