using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Specialized;
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
        private AppSettings _appSettings;  
        private bool _isDarkTheme;
        #endregion

        #region Constructor
        public SettingsViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;

            // Commands
            ChangeActiveJobCommand = new Command<JobProfile>(OnChangeActiveJob);
            AddJobCommand = new Command(OnAddJob);
            EditActiveJobCommand = new Command(OnEditActiveJob);
            DeleteJobCommand = new Command<JobProfile>(OnDeleteJob);

            AddOBRateCommand = new Command(OnAddOBRate, () => HasActiveJob);
            DeleteOBRateCommand = new Command<OBRate>(OnDeleteOBRate);

            LoadJobs();
            LoadOBRates();
            LoadAppSettings();
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

        // Tema-properties
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ThemeDescription));
                    OnThemeChanged(value);  // Spara och applicera tema
                }
            }
        }

        public string ThemeDescription => IsDarkTheme ? "Mörkt utseende aktiverat" : "Ljust utseende aktiverat";
        #endregion

        #region Commands
        public ICommand ChangeActiveJobCommand { get; }
        public ICommand AddJobCommand { get; }
        public ICommand EditActiveJobCommand { get; }

        public ICommand DeleteJobCommand { get; }
        public ICommand AddOBRateCommand { get; }
        public ICommand DeleteOBRateCommand { get; }

        #endregion

        #region Methods
        private void LoadJobs()
        {
            var jobs = _databaseService.JobProfiles.GetJobProfiles();

            AllJobs = new ObservableCollection<JobProfile>(jobs);
            ActiveJob = jobs.FirstOrDefault(j => j.IsActive);

            OnPropertyChanged(nameof(HasActiveJob));
            OnPropertyChanged(nameof(HasMultipleJobs));
            OnPropertyChanged(nameof(ActiveJobText));

            // Uppdatera command states
            ((Command)EditActiveJobCommand).ChangeCanExecute();
            ((Command)AddOBRateCommand).ChangeCanExecute(); 

            LoadOBRates(); // Ladda OB-regler när jobb ändras
        }

        private void LoadOBRates()
        {
            if (ActiveJob != null)
            {
                var obRates = _databaseService.OBRates.GetOBRates(ActiveJob.Id);

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
                        _databaseService.JobProfiles.SaveJobProfile(job);
                    }
                }

                // Aktivera valt jobb (UI uppdateras automatiskt)
                selectedJob.IsActive = true; // 🔥 Triggar PropertyChanged
                _databaseService.JobProfiles.SaveJobProfile(selectedJob);

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

        private async void OnDeleteJob(JobProfile jobToDelete)
        {
            if (jobToDelete == null)
                return;

            bool isLastJob = AllJobs.Count == 1;
            bool isActiveJob = jobToDelete.IsActive;

            // Varning för aktivt jobb (men tillåt om det är sista)
            if (isActiveJob && !isLastJob)
            {
                await Shell.Current.DisplayAlert(
                    "Aktivt jobb",
                    "Du raderar det aktiva jobbet. Välj ett annat jobb som aktivt först, eller radera alla jobb.",
                    "OK");
                return;
            }

            // Extra varning för sista jobbet
            string warningMessage = isLastJob
                ? $"Du raderar ditt SISTA jobb '{jobToDelete.JobTitle}'.\n\nDu kommer behöva skapa ett nytt jobb för att använda appen.\n\nFortsätt?"
                : $"Vill du radera '{jobToDelete.JobTitle}' från {jobToDelete.Workplace}?\n\nDetta raderar alla relaterade pass och data.";

            bool confirm = await Shell.Current.DisplayAlert(
                isLastJob ? "⚠️ Radera sista jobb" : "Radera jobb",
                warningMessage,
                "Ja, radera",
                "Avbryt");

            if (!confirm)
                return;

            try
            {
                // Radera jobbet (med alla relaterade data)
                _databaseService.JobProfiles.DeleteJobProfile(jobToDelete.Id);

                // Uppdatera UI
                LoadJobs();

                string successMessage = isLastJob
                    ? "Sista jobbet raderat. Skapa ett nytt jobb för att fortsätta använda appen."
                    : "Jobbet har raderats.";

                await Shell.Current.DisplayAlert("Raderat", successMessage, "OK");

                // Om inga jobb kvar, navigera till AddJob
                if (AllJobs.Count == 0)
                {
                    await Shell.Current.GoToAsync($"//{nameof(AddJobPage)}");
                }
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
                int deletedRows = _databaseService.OBRates.DeleteOBRate(obRate.Id);

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

        #region Theme Methods
        private void LoadAppSettings()
        {
            try
            {
                _appSettings = _databaseService.AppSettings.GetAppSettings();
                _isDarkTheme = _appSettings.IsDarkTheme;
                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(ThemeDescription));

                // Applicera tema direkt
                ApplyTheme(_isDarkTheme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL vid laddning av app-inställningar: {ex.Message}");
                // Fallback till ljust tema
                _isDarkTheme = false;
                ApplyTheme(false);
            }
        }

        private async void OnThemeChanged(bool isDarkTheme)
        {
            try
            {
                _appSettings.IsDarkTheme = isDarkTheme;
                _databaseService.AppSettings.SaveAppSettings(_appSettings);
                ApplyTheme(isDarkTheme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL vid tema-ändring: {ex.Message}");
                await Shell.Current.DisplayAlert("Fel", "Kunde inte spara tema-inställning", "OK");
            }
        }

        private void ApplyTheme(bool isDarkTheme)
        {
            // Sätt app-tema
            Application.Current.UserAppTheme = isDarkTheme ? AppTheme.Dark : AppTheme.Light;
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
