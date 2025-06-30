using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Views.Pages
{
    [QueryProperty(nameof(JobId), "jobId")]
    public partial class EditJobPage : ContentPage
    {
        private readonly EditJobViewModel _viewModel;

        public string JobId
        {
            set => _viewModel.LoadJob(int.Parse(value));
        }

        public EditJobPage(EditJobViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }
    }
}
