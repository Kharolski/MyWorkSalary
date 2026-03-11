using MyWorkSalary.Helpers;
using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Services.Premium;
using MyWorkSalary.Services.Templates;
using MyWorkSalary.Views.Pages;
using MyWorkSalary.Views.Settings;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        #region Private Fields
        private readonly DatabaseService _databaseService;
        private JobProfile _activeJob;
        private ObservableCollection<JobProfile> _allJobs;
        private bool _isChangingJob = false;
        private AppSettings _appSettings;
        private readonly IOBEventService _obEventService;
        private readonly IOnCallRecalcService _onCallRecalcService;
        private readonly IFeatureLockService _featureLockService;
        private readonly IPremiumService _premiumService;
        private readonly AdService _adService;

        private bool _isDarkTheme;
        private LanguageOption _selectedLanguage;
        #endregion

        #region Constructor
        public SettingsViewModel(
            DatabaseService databaseService, 
            IOBEventService obEventService, 
            IOnCallRecalcService onCallRecalcService,
            IFeatureLockService featureLockService,
            IPremiumService premiumService,
            AdService adService)
        {
            _databaseService = databaseService;
            _obEventService = obEventService;
            _onCallRecalcService = onCallRecalcService;
            _featureLockService = featureLockService;
            _premiumService = premiumService;
            _adService = adService;

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

            AddFreeOBTemplateCommand = new Command(async () => await OnAddFreeOBTemplate());

            LoadJobs();
            LoadOBRates();
            LoadAppSettings();
            
            // Visa banner när settings laddas
            _adService.ShowBanner();
        }
        #endregion

        #region Properties
        public JobProfile ActiveJob
        {
            get => _activeJob;
            set
            {
                _activeJob = value;

                // Extra shift
                _extraShiftAmountText = null;
                _selectedExtraShiftType = null;

                OnPropertyChanged();
                OnPropertyChanged(nameof(ActiveJobText));
                OnPropertyChanged(nameof(HasActiveJob));

                // Extra shift 
                OnPropertyChanged(nameof(ExtraShiftEnabled));
                OnPropertyChanged(nameof(ExtraShiftPayType));
                OnPropertyChanged(nameof(ExtraShiftAmount));
                OnPropertyChanged(nameof(ExtraShiftSummaryText));
                OnPropertyChanged(nameof(ShowExtraShiftSettings));
                OnPropertyChanged(nameof(ExtraShiftAmountLabelText));
                OnPropertyChanged(nameof(ExtraShiftAmountText));
                OnPropertyChanged(nameof(SelectedExtraShiftType));
                OnPropertyChanged(nameof(ExtraShiftTypes));

                // On-call / jour
                _onCallStandbyAmountText = null;
                _onCallActiveCustomRateText = null;
                _selectedOnCallStandbyPayType = null;
                _selectedOnCallActivePayMode = null;
                _selectedOnCallRecalcOption = null;

                InitOnCallOptionTexts();
                NotifyOnCallBindings();

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
                OnPropertyChanged(nameof(HasNoJobs));
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

        public ICommand OpenJobSettingsCommand => new Command(async () =>
        {
            try
            {
                IsBusy = true;
                await Shell.Current.GoToAsync(nameof(JobSettingsPage));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigate to JobSettingsPage Error: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        });
        public ICommand OpenAboutAppCommand => new Command(async () =>
        {
            await Shell.Current.GoToAsync(nameof(AboutAppPage));
        });

        #endregion

        #region Methods
        private void LoadJobs()
        {
            var jobs = _databaseService.JobProfiles.GetJobProfiles();
            AllJobs = new ObservableCollection<JobProfile>(jobs);
            ActiveJob = jobs.FirstOrDefault(j => j.IsActive);

            // Så att pickern får rätt text direkt
            RefreshExtraShiftTypeTexts();
            OnPropertyChanged(nameof(SelectedExtraShiftType));

            OnPropertyChanged(nameof(HasActiveJob));
            OnPropertyChanged(nameof(HasMultipleJobs));
            OnPropertyChanged(nameof(ActiveJobText));
            OnPropertyChanged(nameof(HasNoJobs));

            ((Command)EditActiveJobCommand).ChangeCanExecute();
            ((Command)AddOBRateCommand).ChangeCanExecute();

            LoadOBRates();
        }

        private void SaveActiveJob()
        {
            if (ActiveJob == null)
                return;

            _databaseService.JobProfiles.SaveJobProfile(ActiveJob);
        }

        public async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;

                // Ladda data i bakgrunden för snabbare UI
                await Task.Run(() =>
                {
                    try
                    {
                        // Ladda jobb
                        var jobs = _databaseService.JobProfiles.GetJobProfiles();
                        var activeJob = jobs.FirstOrDefault(j => j.IsActive);

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            AllJobs = new ObservableCollection<JobProfile>(jobs);
                            ActiveJob = activeJob;
                        });

                        // Ladda andra inställningar
                        LoadOBRates();
                        LoadAppSettings();
                    }
                    catch (Exception dataEx)
                    {
                        throw; // Kasta vidare för att hanteras i yttre catch
                    }
                });

                // Visa banner efter att data har laddats (om inte premium)
                try
                {
                    _adService.ShowBanner();
                }
                catch (Exception adEx)
                {
                    // Fortsätt även om banner misslyckas
                }
            }
            catch (Exception ex)
            {
                throw; // Kasta vidare för att hanteras i SettingsPage
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void RefreshActiveJob()
        {
            LoadJobs();
            
            // Visa banner när settings uppdateras (AdService hanterar premium-kontroll)
            _adService.ShowBanner();
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
                        job.IsActive = false; 
                        _databaseService.JobProfiles.SaveJobProfile(job);
                    }
                }

                // Aktivera valt jobb (UI uppdateras automatiskt)
                selectedJob.IsActive = true; 
                _databaseService.JobProfiles.SaveJobProfile(selectedJob);

                // Uppdatera ActiveJob property
                ActiveJob = selectedJob;

                // Uppdatera global active job provider så alla pages vet om bytet
                ActiveJobProvider.Current = selectedJob;

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
            // Hämta antal jobb
            int currentJobs = AllJobs?.Count ?? 0;

            // Fråga FeatureLockService
            if (!_featureLockService.CanAddMoreJobs(currentJobs))
            {
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("PremiumRequiredTitle"),
                    LocalizationHelper.Translate("PremiumRequiredAddJobMessage"),
                    LocalizationHelper.Translate("OK"));

                return;
            }

            // Tillåt navigering
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
        // Flyttad till partial-fil: ViewModels/Settings/SettingsViewModel.OB.cs
        #endregion

        #region Extra shift settings (JobProfile)
        // Flyttad till partial-fil: ViewModels/Settings/SettingsViewModel.ExtraShift.cs
        #endregion

        #region OnCall / Jour Settings
        // Flyttad till partial-fil: ViewModels/Settings/SettingsViewModel.OnCall.cs
        #endregion

        #region Theme Methods
        // Flyttad till partial-fil: ViewModels/Settings/SettingsViewModel.Theme.cs
        #endregion

        #region Language Methods
        // Flyttad till partial-fil: ViewModels/Settings/SettingsViewModel.Language.cs
        #endregion

        #region Premium Service
        // Properties
        public bool IsPremiumOrSubscriber => _premiumService.IsPremium || _premiumService.IsSubscriber;
        public bool IsFreeUser => !IsPremiumOrSubscriber;
        public string FreeOBTemplateButtonText => HasOBRates
            ? Resources.Resx.Resources.ReplaceFreeTemplate  // "Ersätt mall"
            : Resources.Resx.Resources.AddFreeOBTemplate;   // "Lägg till gratis mall"

        // Commands
        public ICommand OpenPremiumPageCommand => new Command(async () =>
        {
            await Shell.Current.GoToAsync(nameof(PremiumInfoPage));
        });
        public ICommand AddFreeOBTemplateCommand { get; }

        // Methods
        private async Task OnAddFreeOBTemplate()
        {
            if (ActiveJob == null)
                return;

            try
            {
                // 0. Radera befintliga regler OM det är "ersätt"
                if (HasOBRates)
                {
                    foreach (var existingRate in OBRates.ToList())
                    {
                        _databaseService.OBRates.DeleteOBRate(existingRate.Id);
                    }
                }

                // 1. Hämta gratis mallen
                var template = TemplateFactory.CreateFreeOBTemplate();

                // 2. Konvertera och spara reglerna
                var obRates = new List<OBRate>();
                foreach (var rule in template.Rules)
                {
                    obRates.Add(new OBRate
                    {
                        JobProfileId = ActiveJob.Id,
                        Name = rule.Name,
                        StartTime = rule.StartTime,
                        EndTime = rule.EndTime,
                        RatePerHour = rule.RatePerHour,
                        Priority = rule.Priority,
                        Category = rule.Category,
                        Monday = rule.Monday,
                        Tuesday = rule.Tuesday,
                        Wednesday = rule.Wednesday,
                        Thursday = rule.Thursday,
                        Friday = rule.Friday,
                        Saturday = rule.Saturday,
                        Sunday = rule.Sunday,
                        IsActive = true
                    });
                }

                // 3. Spara alla regler
                foreach (var obRate in obRates)
                {
                    _databaseService.OBRates.SaveOBRate(obRate);
                }

                // 4. Uppdatera beräkningar
                await _obEventService.RebuildForJobLastMonths(ActiveJob.Id, 4);

                // 5. Ladda om UI
                LoadOBRates();

                // 6. Visa bekräftelse
                await Shell.Current.DisplayAlert(
                    Resources.Resx.Resources.SuccessTitle,
                    Resources.Resx.Resources.FreeOBTemplateAddedMessage,
                    Resources.Resx.Resources.Ok);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    Resources.Resx.Resources.ErrorTitle,
                    Resources.Resx.Resources.FreeOBTemplateFailedMessage,
                    Resources.Resx.Resources.Ok);
            }
        }
        #endregion
    }

    public class LanguageOption
    {
        public string DisplayName { get; set; }
        public string Code { get; set; }
    }
}