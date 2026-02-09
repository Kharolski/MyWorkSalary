using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Views.Pages;
using MyWorkSalary.Views.Pages.Templates;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
                InitOnCallOptionTexts();

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

        #region Extra shift settings (JobProfile)
        public class ExtraShiftTypeOption
        {
            public ExtraShiftPayType Type { get; set; }
            public string Text { get; set; } = "";
        }

        public ObservableCollection<ExtraShiftTypeOption> ExtraShiftTypes { get; } =
            new ObservableCollection<ExtraShiftTypeOption>
            {
                new ExtraShiftTypeOption { Type = ExtraShiftPayType.PerHour, Text = Resources.Resx.Resources.ExtraShiftPayType_PerHour},
                new ExtraShiftTypeOption { Type = ExtraShiftPayType.FixedAmount, Text = Resources.Resx.Resources.ExtraShiftPayType_FixedAmount},
            };

        private ExtraShiftTypeOption _selectedExtraShiftType;
        public ExtraShiftTypeOption SelectedExtraShiftType
        {
            get
            {
                if (_selectedExtraShiftType != null)
                    return _selectedExtraShiftType;
                _selectedExtraShiftType = ExtraShiftTypes.FirstOrDefault(x => x.Type == ExtraShiftPayType)
                                         ?? ExtraShiftTypes.First();
                return _selectedExtraShiftType;
            }
            set
            {
                if (value == null)
                    return;
                _selectedExtraShiftType = value;
                ExtraShiftPayType = value.Type; 
                OnPropertyChanged();
            }
        }

        private void RefreshExtraShiftTypeTexts()
        {
            // Bygg om texterna (så UI alltid uppdateras)
            ExtraShiftTypes.Clear();

            ExtraShiftTypes.Add(new ExtraShiftTypeOption
            {
                Type = ExtraShiftPayType.PerHour,
                Text = Resources.Resx.Resources.ExtraShiftPayType_PerHour
            });

            ExtraShiftTypes.Add(new ExtraShiftTypeOption
            {
                Type = ExtraShiftPayType.FixedAmount,
                Text = Resources.Resx.Resources.ExtraShiftPayType_FixedAmount
            });

            // Tvinga omval baserat på jobbet
            _selectedExtraShiftType = null;
            OnPropertyChanged(nameof(ExtraShiftTypes));
            OnPropertyChanged(nameof(SelectedExtraShiftType));
        }

        public bool ExtraShiftEnabled
        {
            get => ActiveJob?.ExtraShiftEnabled ?? false;
            set
            {
                if (ActiveJob == null)
                    return;
                if (ActiveJob.ExtraShiftEnabled == value)
                    return;

                ActiveJob.ExtraShiftEnabled = value;
                SaveActiveJob();

                OnPropertyChanged();
                OnPropertyChanged(nameof(ExtraShiftSummaryText));
                OnPropertyChanged(nameof(ShowExtraShiftSettings));
            }
        }

        public bool ShowExtraShiftSettings => HasActiveJob && ExtraShiftEnabled;

        public ExtraShiftPayType ExtraShiftPayType
        {
            get => ActiveJob?.ExtraShiftPayType ?? ExtraShiftPayType.PerHour;
            set
            {
                if (ActiveJob == null)
                    return;
                if (ActiveJob.ExtraShiftPayType == value)
                    return;

                ActiveJob.ExtraShiftPayType = value;
                SaveActiveJob();

                OnPropertyChanged();
                OnPropertyChanged(nameof(ExtraShiftSummaryText));
                OnPropertyChanged(nameof(ExtraShiftAmountLabelText));
                OnPropertyChanged(nameof(SelectedExtraShiftType));
            }
        }

        public decimal ExtraShiftAmount
        {
            get => ActiveJob?.ExtraShiftAmount ?? 0m;
            set
            {
                if (ActiveJob == null)
                    return;
                if (ActiveJob.ExtraShiftAmount == value)
                    return;

                ActiveJob.ExtraShiftAmount = value;
                SaveActiveJob();

                OnPropertyChanged();
                OnPropertyChanged(nameof(ExtraShiftSummaryText));
            }
        }

        // Label i UI (inte "kr/timme" – vi låter valutan styra)
        public string ExtraShiftAmountLabelText =>
            ExtraShiftPayType == ExtraShiftPayType.PerHour
                ? Resources.Resx.Resources.ExtraShiftSettings_AmountPerHour
                : Resources.Resx.Resources.ExtraShiftSettings_AmountFixed;

        // En liten sammanfattning (valuta format)
        public string ExtraShiftSummaryText
        {
            get
            {
                if (ActiveJob == null || !ActiveJob.ExtraShiftEnabled || ActiveJob.ExtraShiftAmount <= 0)
                    return "—";

                var currency = string.IsNullOrWhiteSpace(ActiveJob.CurrencyCode) ? "SEK" : ActiveJob.CurrencyCode;
                var money = CurrencyHelper.FormatCurrency(ActiveJob.ExtraShiftAmount, currency);

                return ExtraShiftPayType == ExtraShiftPayType.PerHour
                    ? string.Format(Resources.Resx.Resources.ExtraShiftSettings_Summary_PerHour, money)
                    : string.Format(Resources.Resx.Resources.ExtraShiftSettings_Summary_Fixed, money);
            }
        }

        private void SaveActiveJob()
        {
            if (ActiveJob == null)
                return;
            _databaseService.JobProfiles.SaveJobProfile(ActiveJob);
        }

        private string _extraShiftAmountText;
        public string ExtraShiftAmountText
        {
            get
            {
                if (_extraShiftAmountText != null)
                    return _extraShiftAmountText;
                _extraShiftAmountText = (ActiveJob?.ExtraShiftAmount ?? 0m).ToString("0.##", CultureInfo.CurrentCulture);
                return _extraShiftAmountText;
            }
            set
            {
                _extraShiftAmountText = value;
                OnPropertyChanged();

                if (ActiveJob == null)
                    return;

                // tolerera både , och .
                var input = (value ?? "").Trim().Replace(",", ".");
                if (decimal.TryParse(input, System.Globalization.NumberStyles.Number,
                                     System.Globalization.CultureInfo.InvariantCulture, out var amount))
                {
                    if (ActiveJob.ExtraShiftAmount != amount)
                    {
                        ActiveJob.ExtraShiftAmount = amount;
                        // Spara helst inte på varje tecken om du vill — men funkar om du accepterar det:
                        SaveActiveJob();
                        OnPropertyChanged(nameof(ExtraShiftSummaryText));
                    }
                }
            }
        }
        #endregion

        #region OnCall / Jour Settings
        public class Option<T>
        {
            public T Value { get; set; }
            public string Text { get; set; } = "";
        }

        public bool OnCallEnabled
        {
            get => ActiveJob?.OnCallEnabled ?? false;
            set
            {
                if (ActiveJob == null)
                    return;
                if (ActiveJob.OnCallEnabled == value)
                    return;

                ActiveJob.OnCallEnabled = value;
                SaveActiveJob();

                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowOnCallSettings));
                OnPropertyChanged(nameof(OnCallSettingsSummaryText));
            }
        }
        public bool ShowOnCallSettings => HasActiveJob && OnCallEnabled;

        // ---- Standby pay type ----
        public ObservableCollection<Option<OnCallStandbyPayType>> OnCallStandbyPayTypes { get; } =
            new ObservableCollection<Option<OnCallStandbyPayType>>();

        private Option<OnCallStandbyPayType> _selectedOnCallStandbyPayType;
        public Option<OnCallStandbyPayType> SelectedOnCallStandbyPayType
        {
            get
            {
                if (_selectedOnCallStandbyPayType != null)
                    return _selectedOnCallStandbyPayType;

                var current = ActiveJob?.OnCallStandbyPayType ?? OnCallStandbyPayType.None;
                _selectedOnCallStandbyPayType = OnCallStandbyPayTypes.FirstOrDefault(x => x.Value.Equals(current))
                                                ?? OnCallStandbyPayTypes.FirstOrDefault();
                return _selectedOnCallStandbyPayType;
            }
            set
            {
                if (value == null || ActiveJob == null)
                    return;
                if (SelectedOnCallStandbyPayType?.Value.Equals(value.Value) == true)
                    return;

                _selectedOnCallStandbyPayType = value;
                ActiveJob.OnCallStandbyPayType = value.Value;
                SaveActiveJob();

                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowOnCallStandbyAmount));
                OnPropertyChanged(nameof(OnCallStandbyAmountLabelText));
                OnPropertyChanged(nameof(OnCallSettingsSummaryText));
            }
        }

        public bool ShowOnCallStandbyAmount =>
            HasActiveJob && OnCallEnabled && (ActiveJob?.OnCallStandbyPayType != OnCallStandbyPayType.None);

        public string OnCallStandbyAmountLabelText
        {
            get
            {
                if (ActiveJob == null)
                    return "";
                return ActiveJob.OnCallStandbyPayType switch
                {
                    OnCallStandbyPayType.PerHour => Resources.Resx.Resources.OnCallSettings_AmountPerHour,
                    OnCallStandbyPayType.PerShift => Resources.Resx.Resources.OnCallSettings_AmountPerShift,
                    _ => Resources.Resx.Resources.OnCallSettings_Amount
                };
            }
        }

        private string _onCallStandbyAmountText;
        public string OnCallStandbyAmountText
        {
            get
            {
                if (_onCallStandbyAmountText != null)
                    return _onCallStandbyAmountText;

                _onCallStandbyAmountText = (ActiveJob?.OnCallStandbyPayAmount ?? 0m)
                    .ToString("0.##", CultureInfo.CurrentCulture);

                return _onCallStandbyAmountText;
            }
            set
            {
                _onCallStandbyAmountText = value;
                OnPropertyChanged();

                if (ActiveJob == null)
                    return;

                var input = (value ?? "").Trim().Replace(",", ".");
                if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                {
                    if (ActiveJob.OnCallStandbyPayAmount != amount)
                    {
                        ActiveJob.OnCallStandbyPayAmount = amount;
                        SaveActiveJob();
                        OnPropertyChanged(nameof(OnCallSettingsSummaryText));
                    }
                }
            }
        }

        // ---- Active pay mode ----
        public ObservableCollection<Option<OnCallActivePayMode>> OnCallActivePayModes { get; } =
            new ObservableCollection<Option<OnCallActivePayMode>>();

        private Option<OnCallActivePayMode> _selectedOnCallActivePayMode;
        public Option<OnCallActivePayMode> SelectedOnCallActivePayMode
        {
            get
            {
                if (_selectedOnCallActivePayMode != null)
                    return _selectedOnCallActivePayMode;

                var current = ActiveJob?.OnCallActivePayMode ?? OnCallActivePayMode.DefaultHourly;
                _selectedOnCallActivePayMode = OnCallActivePayModes.FirstOrDefault(x => x.Value.Equals(current))
                                               ?? OnCallActivePayModes.FirstOrDefault();
                return _selectedOnCallActivePayMode;
            }
            set
            {
                if (value == null || ActiveJob == null)
                    return;
                if (SelectedOnCallActivePayMode?.Value.Equals(value.Value) == true)
                    return;

                _selectedOnCallActivePayMode = value;
                ActiveJob.OnCallActivePayMode = value.Value;
                SaveActiveJob();

                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowOnCallActiveCustomRate));
                OnPropertyChanged(nameof(OnCallSettingsSummaryText));
            }
        }

        public bool ShowOnCallActiveCustomRate =>
            HasActiveJob && OnCallEnabled && (ActiveJob?.OnCallActivePayMode == OnCallActivePayMode.CustomHourly);

        private string _onCallActiveCustomRateText;
        public string OnCallActiveCustomRateText
        {
            get
            {
                if (_onCallActiveCustomRateText != null)
                    return _onCallActiveCustomRateText;

                _onCallActiveCustomRateText = (ActiveJob?.OnCallActiveHourlyRate ?? 0m)
                    .ToString("0.##", CultureInfo.CurrentCulture);
                return _onCallActiveCustomRateText;
            }
            set
            {
                _onCallActiveCustomRateText = value;
                OnPropertyChanged();

                if (ActiveJob == null)
                    return;

                var input = (value ?? "").Trim().Replace(",", ".");
                if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                {
                    if (ActiveJob.OnCallActiveHourlyRate != amount)
                    {
                        ActiveJob.OnCallActiveHourlyRate = amount;
                        SaveActiveJob();
                        OnPropertyChanged(nameof(OnCallSettingsSummaryText));
                    }
                }
            }
        }

        // ---- Recalc options ----
        public ObservableCollection<Option<int>> OnCallRecalcOptions { get; } =
            new ObservableCollection<Option<int>>();

        private Option<int> _selectedOnCallRecalcOption;
        public Option<int> SelectedOnCallRecalcOption
        {
            get
            {
                if (_selectedOnCallRecalcOption != null)
                    return _selectedOnCallRecalcOption;

                var current = ActiveJob?.OnCallRecalcMonths ?? 0;
                _selectedOnCallRecalcOption = OnCallRecalcOptions.FirstOrDefault(x => x.Value == current)
                                              ?? OnCallRecalcOptions.FirstOrDefault();
                return _selectedOnCallRecalcOption;
            }
            set
            {
                if (value == null || ActiveJob == null)
                    return;
                if (ActiveJob.OnCallRecalcMonths == value.Value)
                    return;

                _selectedOnCallRecalcOption = value;
                ActiveJob.OnCallRecalcMonths = value.Value;
                SaveActiveJob();

                OnPropertyChanged();
                OnPropertyChanged(nameof(CanRecalcOnCall));
                OnPropertyChanged(nameof(OnCallSettingsSummaryText));
            }
        }

        public bool CanRecalcOnCall => HasActiveJob && OnCallEnabled && (ActiveJob?.OnCallRecalcMonths ?? 0) > 0;

        public ICommand RecalcOnCallCommand => new Command(async () =>
        {
            // v2: vi kopplar detta senare till “rebuild OB events + jour snapshots”
            await Shell.Current.DisplayAlert(
                Resources.Resx.Resources.InfoTitle,
                Resources.Resx.Resources.OnCallSettings_RecalcNotReadyYet,
                Resources.Resx.Resources.Ok);
        });

        // ---- Summary ----
        public string OnCallSettingsSummaryText
        {
            get
            {
                if (ActiveJob == null || !ActiveJob.OnCallEnabled)
                    return "—";

                var currency = string.IsNullOrWhiteSpace(ActiveJob.CurrencyCode) ? "SEK" : ActiveJob.CurrencyCode;

                // Standby text
                string standbyText = ActiveJob.OnCallStandbyPayType switch
                {
                    OnCallStandbyPayType.None => Resources.Resx.Resources.OnCallSettings_Standby_None,
                    OnCallStandbyPayType.PerHour => $"{CurrencyHelper.FormatCurrency(ActiveJob.OnCallStandbyPayAmount, currency)}/tim",
                    OnCallStandbyPayType.PerShift => $"{CurrencyHelper.FormatCurrency(ActiveJob.OnCallStandbyPayAmount, currency)}/pass",
                    _ => "—"
                };

                // Active text
                string activeText = ActiveJob.OnCallActivePayMode switch
                {
                    OnCallActivePayMode.DefaultHourly => Resources.Resx.Resources.OnCallSettings_Active_DefaultHourly,
                    OnCallActivePayMode.CustomHourly => $"{Resources.Resx.Resources.OnCallSettings_Active_CustomHourly}: {CurrencyHelper.FormatCurrency(ActiveJob.OnCallActiveHourlyRate, currency)}/tim",
                    _ => "—"
                };

                var recalc = ActiveJob.OnCallRecalcMonths;
                var recalcText = recalc <= 0
                    ? Resources.Resx.Resources.OnCallSettings_Recalc_None
                    : string.Format(Resources.Resx.Resources.OnCallSettings_Recalc_LastXMonths, recalc);

                return $"{Resources.Resx.Resources.OnCallSettings_StandbyLabel}: {standbyText} • {activeText} • {recalcText}";
            }
        }

        private void InitOnCallOptionTexts()
        {
            OnCallStandbyPayTypes.Clear();
            OnCallStandbyPayTypes.Add(new Option<OnCallStandbyPayType> { Value = OnCallStandbyPayType.None, Text = Resources.Resx.Resources.OnCallPayType_None });
            OnCallStandbyPayTypes.Add(new Option<OnCallStandbyPayType> { Value = OnCallStandbyPayType.PerHour, Text = Resources.Resx.Resources.OnCallPayType_PerHour });
            OnCallStandbyPayTypes.Add(new Option<OnCallStandbyPayType> { Value = OnCallStandbyPayType.PerShift, Text = Resources.Resx.Resources.OnCallPayType_PerShift });

            OnCallActivePayModes.Clear();
            OnCallActivePayModes.Add(new Option<OnCallActivePayMode> { Value = OnCallActivePayMode.DefaultHourly, Text = Resources.Resx.Resources.OnCallActivePay_DefaultHourly });
            OnCallActivePayModes.Add(new Option<OnCallActivePayMode> { Value = OnCallActivePayMode.CustomHourly, Text = Resources.Resx.Resources.OnCallActivePay_CustomHourly });

            OnCallRecalcOptions.Clear();
            OnCallRecalcOptions.Add(new Option<int> { Value = 0, Text = Resources.Resx.Resources.Recalc_None });
            OnCallRecalcOptions.Add(new Option<int> { Value = 1, Text = Resources.Resx.Resources.Recalc_1Month });
            OnCallRecalcOptions.Add(new Option<int> { Value = 2, Text = Resources.Resx.Resources.Recalc_2Months });
            OnCallRecalcOptions.Add(new Option<int> { Value = 3, Text = Resources.Resx.Resources.Recalc_3Months });

            // tvinga omval
            _selectedOnCallStandbyPayType = null;
            _selectedOnCallActivePayMode = null;
            _selectedOnCallRecalcOption = null;

            OnPropertyChanged(nameof(OnCallStandbyPayTypes));
            OnPropertyChanged(nameof(OnCallActivePayModes));
            OnPropertyChanged(nameof(OnCallRecalcOptions));

            OnPropertyChanged(nameof(SelectedOnCallStandbyPayType));
            OnPropertyChanged(nameof(SelectedOnCallActivePayMode));
            OnPropertyChanged(nameof(SelectedOnCallRecalcOption));
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

            // Uppdatera ExtraShift picker-texter
            RefreshExtraShiftTypeTexts();
            OnPropertyChanged(nameof(ExtraShiftAmountLabelText));
            OnPropertyChanged(nameof(ExtraShiftSummaryText));
            OnPropertyChanged(nameof(SelectedExtraShiftType));

            // Uppdatera OnCall picker texter
            InitOnCallOptionTexts();
            OnPropertyChanged(nameof(OnCallStandbyAmountLabelText));
            OnPropertyChanged(nameof(OnCallSettingsSummaryText));

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
