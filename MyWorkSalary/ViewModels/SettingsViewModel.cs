using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using MyWorkSalary.Models;
using MyWorkSalary.Services;
using MyWorkSalary.Views.Pages;

namespace MyWorkSalary.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly DatabaseService _databaseService;
        private JobProfile _activeJob;
        private ObservableCollection<JobProfile> _allJobs;
        private bool _isChangingJob = false;
        #endregion

        #region Constructor
        public SettingsViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;

            // Commands
            ChangeActiveJobCommand = new Command<JobProfile>(OnChangeActiveJob);
            AddJobCommand = new Command(OnAddJob);
            EditActiveJobCommand = new Command(OnEditActiveJob);
            DeleteActiveJobCommand = new Command(OnDeleteActiveJob);

            LoadJobs();
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
                OnPropertyChanged(nameof(ActiveJobText));
                OnPropertyChanged(nameof(HasActiveJob));
            }
        }

        public ObservableCollection<JobProfile> AllJobs
        {
            get => _allJobs;
            set
            {
                _allJobs = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasMultipleJobs));
            }
        }

        public string ActiveJobText => ActiveJob != null
            ? $"{ActiveJob.JobTitle} - {ActiveJob.Workplace}"
            : "Inget aktivt jobb";

        public bool HasActiveJob => ActiveJob != null;
        public bool HasMultipleJobs => AllJobs?.Count > 1;
        #endregion

        #region Commands
        public ICommand ChangeActiveJobCommand { get; }
        public ICommand AddJobCommand { get; }
        public ICommand EditActiveJobCommand { get; }
        public ICommand DeleteActiveJobCommand { get; }
        #endregion

        #region Methods
        private void LoadJobs()
        {
            System.Diagnostics.Debug.WriteLine("LoadJobs() körs!");
            var jobs = _databaseService.GetJobProfiles();

            System.Diagnostics.Debug.WriteLine($"Hämtade {jobs.Count()} jobb från DB:");
            foreach (var job in jobs)
            {
                System.Diagnostics.Debug.WriteLine($"  - {job.JobTitle} (Active: {job.IsActive})");
            }

            AllJobs = new ObservableCollection<JobProfile>(jobs);
            ActiveJob = jobs.FirstOrDefault(j => j.IsActive);

            System.Diagnostics.Debug.WriteLine($"UI visar nu {AllJobs.Count} jobb");
        }

        public void RefreshActiveJob()
        {
            System.Diagnostics.Debug.WriteLine("RefreshActiveJob() körs!");
            LoadJobs();
        }

        private async void OnChangeActiveJob(JobProfile selectedJob)
        {
            if (_isChangingJob)
            {
                return;
            }

            if (selectedJob == null || selectedJob.IsActive)
            {
                return;
            }

            _isChangingJob = true;

            try
            {
                // Inaktivera alla jobb (UI uppdateras automatiskt via INotifyPropertyChanged)
                foreach (var job in AllJobs)
                {
                    if (job.IsActive)
                    {
                        job.IsActive = false; // 🔥 Triggar PropertyChanged
                        _databaseService.SaveJobProfile(job);
                    }
                }

                // Aktivera valt jobb (UI uppdateras automatiskt)
                selectedJob.IsActive = true; // 🔥 Triggar PropertyChanged
                _databaseService.SaveJobProfile(selectedJob);

                // Uppdatera ActiveJob property
                ActiveJob = selectedJob;

                await Shell.Current.DisplayAlert("Framgång", $"Bytte till: {selectedJob.JobTitle}", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL vid jobbyte: {ex.Message}");
                await Shell.Current.DisplayAlert("Fel", $"Kunde inte byta jobb: {ex.Message}", "OK");
            }
            finally
            {
                _isChangingJob = false;
            }
        }

        private async void OnAddJob()
        {
            await Shell.Current.GoToAsync(nameof(AddJobPage));
        }

        private async void OnEditActiveJob()
        {
            if (ActiveJob != null)
            {
                await Shell.Current.GoToAsync($"{nameof(EditJobPage)}?jobId={ActiveJob.Id}");
            }
        }

        private async void OnDeleteActiveJob()
        {
            if (ActiveJob == null)
                return;

            // Bekräftelse med jobbnamn
            bool confirm = await Shell.Current.DisplayAlert(
                "Radera jobb",
                $"Vill du radera '{ActiveJob.JobTitle}' från {ActiveJob.Workplace}?\n\nDetta går inte att ångra.",
                "Ja, radera",
                "Avbryt");

            if (!confirm)
                return;

            try
            {
                var allJobs = _databaseService.GetJobProfiles();

                // Ta bort jobbet
                _databaseService.DeleteJobProfile(ActiveJob.Id);

                // Om det fanns fler jobb, sätt nästa som aktivt
                var remainingJobs = allJobs.Where(j => j.Id != ActiveJob.Id).ToList();
                if (remainingJobs.Any())
                {
                    var nextActiveJob = remainingJobs.First();
                    nextActiveJob.IsActive = true;
                    _databaseService.SaveJobProfile(nextActiveJob);
                }

                // Uppdatera UI
                LoadJobs();

                await Shell.Current.DisplayAlert("Raderat", "Jobbet har raderats.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fel", $"Kunde inte radera jobbet: {ex.Message}", "OK");
            }
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
