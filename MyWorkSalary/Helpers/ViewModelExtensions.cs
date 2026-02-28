using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Helpers
{
    public static class ViewModelExtensions
    {
        public static async Task LoadDataAsync(this BaseViewModel viewModel, Func<Task> loadDataAction)
        {
            try
            {
                viewModel.IsBusy = true;
                await loadDataAction();
            }
            finally
            {
                viewModel.IsBusy = false;
            }
        }

        public static async Task LoadDataAsync(this BaseViewModel viewModel, Action loadDataAction)
        {
            try
            {
                viewModel.IsBusy = true;
                await Task.Run(loadDataAction);
            }
            finally
            {
                viewModel.IsBusy = false;
            }
        }
    }
}
