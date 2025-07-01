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

            AddOBRateCommand = new Command(OnAddOBRate, () => HasActiveJob);
            DeleteOBRateCommand = new Command<OBRate>(OnDeleteOBRate);

            LoadJobs();
            LoadOBRates();
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

        // Property för OB-regler
        public ObservableCollection<OBRate> OBRates { get; } = new ObservableCollection<OBRate>();
        public bool HasOBRates => OBRates?.Count > 0;
        #endregion

        #region Commands
        public ICommand ChangeActiveJobCommand { get; }
        public ICommand AddJobCommand { get; }
        public ICommand EditActiveJobCommand { get; }
        public ICommand DeleteActiveJobCommand { get; }

        public ICommand AddOBRateCommand { get; }
        public ICommand DeleteOBRateCommand { get; }

        #endregion

        #region Methods
        private void LoadJobs()
        {
            var jobs = _databaseService.GetJobProfiles();

            foreach (var job in jobs)
            {
                System.Diagnostics.Debug.WriteLine($"  - {job.JobTitle} (Active: {job.IsActive})");
            }

            AllJobs = new ObservableCollection<JobProfile>(jobs);
            ActiveJob = jobs.FirstOrDefault(j => j.IsActive);

            OnPropertyChanged(nameof(HasActiveJob));
            OnPropertyChanged(nameof(HasMultipleJobs));
            OnPropertyChanged(nameof(ActiveJobText));

            // Uppdatera command states
            ((Command)EditActiveJobCommand).ChangeCanExecute();
            ((Command)DeleteActiveJobCommand).ChangeCanExecute();
            ((Command)AddOBRateCommand).ChangeCanExecute(); 

            LoadOBRates(); // Ladda OB-regler när jobb ändras
        }

        private void LoadOBRates()
        {
            if (ActiveJob != null)
            {
                var obRates = _databaseService.GetOBRates(ActiveJob.Id);

                OBRates.Clear();
                foreach (var rate in obRates)
                {
                    OBRates.Add(rate);
                }

                OnPropertyChanged(nameof(HasOBRates));
            }
            else
            {
                OBRates.Clear();
                OnPropertyChanged(nameof(HasOBRates));
            }
        }

        public void RefreshActiveJob()
        {
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

                // Uppdatera OB property för aktiv job
                LoadOBRates();

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

        private async void OnAddOBRate()
        {
            await Shell.Current.GoToAsync(nameof(AddOBRatePage));
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

        private async void OnDeleteOBRate(OBRate obRate)
        {
            if (obRate == null)
                return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Radera OB-regel",
                $"Vill du radera '{obRate.Name}'?",
                "Ja", "Nej");

            if (confirm)
            {
                int deletedRows = _databaseService.DeleteOBRate(obRate.Id);

                if (deletedRows > 0)
                {
                    LoadOBRates();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Fel", "Kunde inte radera OB-regeln", "OK");
                }
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
