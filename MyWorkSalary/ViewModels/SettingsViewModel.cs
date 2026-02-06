using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Views.Pages;
using MyWorkSalary.Views.Pages.Templates;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

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
        private readonly IOBEventService _obEventService;

        private bool _isDarkTheme;
        private LanguageOption _selectedLanguage;
        #endregion

        #region Constructor
        public SettingsViewModel(DatabaseService databaseService, IOBEventService obEventService)
        {
            _databaseService = databaseService;
            _obEventService = obEventService;

            TranslationManager.Instance.CultureChanged += OnCultureChanged;

            // Commands
            ChangeActiveJobCommand = new Command<JobProfile>(OnChangeActiveJob);
            AddJobCommand = new Command(OnAddJob);
            EditActiveJobCommand = new Command(OnEditActiveJob);
            DeleteJobCommand = new Command<JobProfile>(OnDeleteJob);

            AddOBRateCommand = new Command(OnAddOBRate, () => HasActiveJob);
            EditOBRateCommand = new Command<OBRate>(OnEditOBRate);
            DeleteOBRateCommand = new Command<OBRate>(OnDeleteOBRate);
            AddOBTemplateCommand = new Command(async () => await OnOpenOBTemplates(false), () => HasActiveJob);
            ReplaceWithTemplateCommand = new Command(async () => await OnOpenOBTemplates(true), () => HasActiveJob && HasOBRates);

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

                ((Command)AddOBRateCommand).ChangeCanExecute();
                ((Command)AddOBTemplateCommand).ChangeCanExecute();
                ((Command)ReplaceWithTemplateCommand).ChangeCanExecute();
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
            : Resources.Resx.Resources.NoActiveJob;

        public bool HasActiveJob => ActiveJob != null;
        public bool HasMultipleJobs => AllJobs?.Count > 1;
        public bool HasNoJobs => AllJobs?.Count == 0;

        // Property för OB-regler
        private ObservableCollection<OBRate> _obRates = new();
        public ObservableCollection<OBRate> OBRates
        {
            get => _obRates;
            set
            {
                _obRates = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasOBRates));
                OnPropertyChanged(nameof(HasNoOBRates));
            }
        }
        public bool HasOBRates => OBRates?.Count > 0;
        public bool HasNoOBRates => !HasOBRates;

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
        public string ThemeDescription => IsDarkTheme
            ? Resources.Resx.Resources.DarkThemeActive
            : Resources.Resx.Resources.LightThemeActive;

        // Tillgängliga språk i appen
        public ObservableCollection<LanguageOption> AvailableLanguages { get; } =
            LanguageProvider.GetAvailableLanguages();

        // Det språk som är valt i pickern
        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value && value != null)
                {
                    ApplyLanguage(value);
                }
            }
        }
        #endregion

        #region Commands
        public ICommand ChangeActiveJobCommand { get; }
        public ICommand AddJobCommand { get; }
        public ICommand EditActiveJobCommand { get; }

        public ICommand DeleteJobCommand { get; }
        public ICommand AddOBRateCommand { get; }
        public ICommand EditOBRateCommand { get; }
        public ICommand DeleteOBRateCommand { get; }

        public ICommand AddOBTemplateCommand { get; }
        public ICommand ReplaceWithTemplateCommand { get; }
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
            OnPropertyChanged(nameof(HasNoJobs));

            ((Command)EditActiveJobCommand).ChangeCanExecute();
            ((Command)AddOBRateCommand).ChangeCanExecute();

            LoadOBRates();
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

                await Shell.Current.DisplayAlert(Resources.Resx.Resources.Success,
                    string.Format(Resources.Resx.Resources.JobChangedMessageFormat, selectedJob.JobTitle),
                    Resources.Resx.Resources.Ok);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    Resources.Resx.Resources.ErrorTitle,
                    string.Format(MyWorkSalary.Resources.Resx.Resources.ChangeJobFailedMessageFormat, ex.Message),
                    Resources.Resx.Resources.Ok);
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

        private async void OnDeleteJob(JobProfile jobToDelete)
        {
            if (jobToDelete == null)
                return;

            bool isLastJob = AllJobs.Count == 1;
            bool isActiveJob = jobToDelete.IsActive;

            // Varningstext om det är det aktiva jobbet och det finns fler jobb kvar
            if (isActiveJob && !isLastJob)
            {
                await Shell.Current.DisplayAlert(
                    Resources.Resx.Resources.ActiveJob,
                    Resources.Resx.Resources.DeleteActiveJobMessage,
                    Resources.Resx.Resources.Ok);
                return;
            }

            // Varningstext (olika för sista jobb / vanligt jobb)
            string warningMessage = isLastJob
                ? string.Format(Resources.Resx.Resources.DeleteLastJobMessageFormat, jobToDelete.JobTitle)
                : string.Format(
                    Resources.Resx.Resources.DeleteJobMessageFormat, jobToDelete.JobTitle, jobToDelete.Workplace);

            // Bekräftelsedialog
            bool confirm = await Shell.Current.DisplayAlert(
                isLastJob
                    ? Resources.Resx.Resources.DeleteLastJobTitle
                    : Resources.Resx.Resources.DeleteJobTitle,
                warningMessage,
                Resources.Resx.Resources.ConfirmDelete,
                Resources.Resx.Resources.Cancel);

            if (!confirm)
                return;

            // Kör radering
            try
            {
                // Radera jobbet (med alla relaterade data)
                _databaseService.JobProfiles.DeleteJobProfile(jobToDelete.Id);

                // Uppdatera UI
                LoadJobs();

                string successMessage = isLastJob
                    ? Resources.Resx.Resources.LastJobDeletedSuccessMessage
                    : Resources.Resx.Resources.JobDeletedSuccessMessage;

                await Shell.Current.DisplayAlert(
                    Resources.Resx.Resources.JobDeletedTitle,
                    successMessage,
                    Resources.Resx.Resources.Ok);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    Resources.Resx.Resources.ErrorTitle,
                    string.Format(Resources.Resx.Resources.DeleteJobFailedMessageFormat, ex.Message),
                    Resources.Resx.Resources.Ok);
            }
        }

        #endregion

        #region OB Methods
        private void LoadOBRates()
        {
            if (ActiveJob != null)
            {
                var obRates = _databaseService.OBRates.GetOBRates(ActiveJob.Id);

                // Sätt rätt valuta för varje OBRate
                foreach (var ob in obRates)
                {
                    ob.CurrencyCode = string.IsNullOrWhiteSpace(ActiveJob.CurrencyCode) ? "SEK" : ActiveJob.CurrencyCode;
                }

                OBRates = new ObservableCollection<OBRate>(obRates);

                ((Command)ReplaceWithTemplateCommand).ChangeCanExecute();
                OnPropertyChanged(nameof(HasOBRates));
            }
            else
            {
                OBRates.Clear();
                OnPropertyChanged(nameof(HasOBRates));
            }
        }

        private async Task OnOpenOBTemplates(bool replaceMode)
        {
            if (ActiveJob == null)
                return;

            var mode = replaceMode ? "replace" : "add";

            // Shell query parameters (jobId + mode)
            await Shell.Current.GoToAsync($"{nameof(OBTemplatesPage)}?jobId={ActiveJob.Id}&mode={mode}");
        }

        private async void OnAddOBRate()
        {
            await Shell.Current.GoToAsync(nameof(AddOBRatePage));
        }

        private async void OnEditOBRate(OBRate obRate)
        {
            if (obRate == null)
                return;

            await Shell.Current.GoToAsync($"{nameof(AddOBRatePage)}?obRateId={obRate.Id}");
        }

        private async void OnDeleteOBRate(OBRate obRate)
        {
            if (obRate == null)
                return;

            // 1) Bekräftelse-dialog
            bool confirm = await Shell.Current.DisplayAlert(
                Resources.Resx.Resources.DeleteOBRateTitle,
                string.Format(Resources.Resx.Resources.DeleteOBRateMessageFormat, obRate.Name),
                Resources.Resx.Resources.ConfirmDelete,
                Resources.Resx.Resources.Cancel);

            if (confirm)
            {
                // Försök radera
                int deletedRows = _databaseService.OBRates.DeleteOBRate(obRate.Id);

                if (deletedRows > 0)
                {
                    // rebuild 4 månader bakåt
                    await _obEventService.RebuildForJobLastMonths(ActiveJob.Id, 4);

                    LoadOBRates();
                }
                else
                {
                    await Shell.Current.DisplayAlert(
                        Resources.Resx.Resources.ErrorTitle,
                        Resources.Resx.Resources.DeleteOBRateFailedMessage,
                        Resources.Resx.Resources.Ok);
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

                // Hämta språk från settings eller fallback
                var savedCode = string.IsNullOrEmpty(_appSettings.LanguageCode)
                    ? "en"
                    : _appSettings.LanguageCode;

                var lang = AvailableLanguages.FirstOrDefault(l => l.Code == savedCode)
                           ?? AvailableLanguages.First();

                ApplyLanguage(lang);

                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(ThemeDescription));

                // Applicera tema direkt
                ApplyTheme(_isDarkTheme);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR loading app settings: {ex.Message}");
                
                // Fallback till ljust tema
                _isDarkTheme = false;
                ApplyTheme(false);

                // fallback språk = engelska
                SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == "en");
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
                await Shell.Current.DisplayAlert(Resources.Resx.Resources.ErrorTitle,
                    Resources.Resx.Resources.ThemeSaveErrorMessage,
                    Resources.Resx.Resources.Ok);
            }
        }

        private void ApplyTheme(bool isDarkTheme)
        {
            // Sätt app-tema
            Application.Current.UserAppTheme = isDarkTheme ? AppTheme.Dark : AppTheme.Light;
        }
        #endregion

        #region Language Methods
        private void ApplyLanguage(LanguageOption lang)
        {
            if (lang == null)
                return;

            _selectedLanguage = lang;
            OnPropertyChanged(nameof(SelectedLanguage));

            // Hämta korrekt kultur (med region) via helper
            var culture = CultureHelper.GetCulture(lang.Code);

            // Uppdatera TranslationManager + trådculture
            TranslationManager.Instance.ChangeCulture(culture);

            // Trigga språkändringshändelsen globalt (så vyer kan reagera)
            LocalizationHelper.NotifyLanguageChanged();

            // Spara inställningen 
            _appSettings.LanguageCode = lang.Code;
            _databaseService.AppSettings.SaveAppSettings(_appSettings);

            // Trigger UI-uppdateringar som behöver omrendering (t.ex CollectionView valutaformat)
            // Om du ser CollectionView med OBRates, rebinda så formaten uppdateras:
            var old = OBRates;
            OBRates = null;
            OBRates = old;

            OnPropertyChanged(nameof(ThemeDescription));
        }

        private void OnCultureChanged(object sender, EventArgs e)
        {
            // Tvinga UI att uppdatera valutor & datum när språk byts
            var currentRates = OBRates.ToList();
            OBRates = new ObservableCollection<OBRate>(currentRates);
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

    public class LanguageOption
    {
        public string DisplayName { get; set; }
        public string Code { get; set; }
    }
}
