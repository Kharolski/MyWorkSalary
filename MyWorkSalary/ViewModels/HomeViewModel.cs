using System.ComponentModel;
using System.Windows.Input;
using MyWorkSalary.Models;
using MyWorkSalary.Services;
using MyWorkSalary.Views.Pages;

namespace MyWorkSalary.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly DatabaseService _databaseService;
        private JobProfile _activeJob;
        private bool _hasActiveJob;
        #endregion

        #region Constructor
        public HomeViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;

            // Commands
            SetupJobCommand = new Command(OnSetupJob);

            // Ladda data
            LoadActiveJob();
        }
        #endregion

        #region Properties
        public JobProfile ActiveJob
        {
            get => _activeJob;
            set
            {
                _activeJob = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WelcomeText));
                OnPropertyChanged(nameof(HasActiveJob));
                OnPropertyChanged(nameof(ShowSetupButton));
            }
        }

        public bool HasActiveJob
        {
            get => _hasActiveJob;
            set
            {
                _hasActiveJob = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowSetupButton));
            }
        }

        public string WelcomeText => HasActiveJob
            ? $"Hej! Du arbetar som {ActiveJob?.JobTitle}"
            : "My Work Salary";

        public bool ShowSetupButton => !HasActiveJob;
        #endregion

        #region Commands
        public ICommand SetupJobCommand { get; }
        #endregion

        #region Methods
        private void LoadActiveJob()
        {
            var jobs = _databaseService.GetJobProfiles();
            var activeJob = jobs.FirstOrDefault(j => j.IsActive);

            // Sätt properties separat för att trigga PropertyChanged
            ActiveJob = activeJob;
            HasActiveJob = activeJob != null;
        }

        public void RefreshData()
        {
            LoadActiveJob();
        }

        private async void OnSetupJob()
        {
            // Gå först till Settings, sedan till AddJobPage
            await Shell.Current.GoToAsync("//SettingsPage");
            await Shell.Current.GoToAsync(nameof(AddJobPage));
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
