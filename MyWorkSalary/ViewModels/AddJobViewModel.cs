using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services;
using MyWorkSalary.Services.Handlers;

namespace MyWorkSalary.ViewModels
{
    public class AddJobViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly DatabaseService _databaseService;
        private string _jobTitle = string.Empty;
        private string _workplace = string.Empty;
        private string _monthlySalary = string.Empty;
        private string _hourlyRate = string.Empty;
        private string _expectedHoursPerMonth = "160";
        private string _taxRate = "33";
        private bool _isSaving = false;
        private CountryOption _selectedCountry;
        private readonly HolidayService _holidayService;

        private DateTime _employmentStartDate = DateTime.Today;
        private string _vacationDaysPerYear = "25";
        private string _initialVacationBalance = string.Empty;
        #endregion

        #region Constructor
        public AddJobViewModel(DatabaseService databaseService, HolidayService holidayService)
        {
            _databaseService = databaseService;
            _holidayService = holidayService;

            // Initiera listor
            InitializeEmploymentAndSalaryTypes();
            InitializeCurrencies();

            Countries = new ObservableCollection<CountryOption>(
                Enum.GetValues(typeof(SupportedCountry))
                    .Cast<SupportedCountry>()
                    .Select(c => new CountryOption { Country = c })
                    .OrderBy(c => c.DisplayName)
            );

            // Standardval
            SelectedCountry = Countries.FirstOrDefault(c => c.Country == SupportedCountry.Sweden)
                          ?? Countries.FirstOrDefault();

            // Commands
            SaveCommand = new Command(OnSave, CanSave);
            CancelCommand = new Command(OnCancel);

            // Lyssna på ändringar för att uppdatera CanSave
            PropertyChanged += (s, e) => ((Command)SaveCommand).ChangeCanExecute();
        }
        #endregion

        #region Properties
        public string JobTitle
        {
            get => _jobTitle;
            set
            {
                _jobTitle = value;
                OnPropertyChanged();
                JobTitleError = string.IsNullOrWhiteSpace(value) 
                    ? Resources.Resx.Resources.Validation_JobTitleRequired 
                    : null;
            }
        }

        public string Workplace
        {
            get => _workplace;
            set
            {
                _workplace = value;
                OnPropertyChanged();
                WorkplaceError = string.IsNullOrWhiteSpace(value) 
                    ? Resources.Resx.Resources.Validation_WorkplaceRequired
                    : null;
            }
        }

        public string MonthlySalary
        {
            get => _monthlySalary;
            set
            {
                _monthlySalary = value;
                OnPropertyChanged();
                MonthlySalaryError = string.IsNullOrWhiteSpace(value) 
                    ? Resources.Resx.Resources.Validation_MonthlySalary 
                    : null;
            }
        }

        public string HourlyRate
        {
            get => _hourlyRate;
            set
            {
                _hourlyRate = value;
                OnPropertyChanged();
                HourlyRateError = string.IsNullOrWhiteSpace(value) 
                    ? Resources.Resx.Resources.Validation_HourlyRate 
                    : null;
            }
        }

        public string ExpectedHoursPerMonth
        {
            get => _expectedHoursPerMonth;
            set
            {
                _expectedHoursPerMonth = value;
                OnPropertyChanged();
            }
        }

        public string TaxRate
        {
            get => _taxRate;
            set
            {
                _taxRate = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<CountryOption> Countries { get; }
        public CountryOption SelectedCountry
        {
            get => _selectedCountry;
            set
            {
                if (_selectedCountry != value)
                {
                    _selectedCountry = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime EmploymentStartDate
        {
            get => _employmentStartDate;
            set
            {
                _employmentStartDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowInitialVacationBalance)); // Uppdatera synlighet
            }
        }

        public string VacationDaysPerYear
        {
            get => _vacationDaysPerYear;
            set
            {
                _vacationDaysPerYear = value;
                OnPropertyChanged();
            }
        }

        public string InitialVacationBalance
        {
            get => _initialVacationBalance;
            set
            {
                _initialVacationBalance = value;
                OnPropertyChanged();
            }
        }

        // Visa bara om anställning > 1 månad
        public bool ShowInitialVacationBalance =>
            DateTime.Today.Subtract(EmploymentStartDate).TotalDays > 30;
        
        // Validation properties
        private string _jobTitleError;
        public string JobTitleError
        {
            get => _jobTitleError;
            set { _jobTitleError = value; OnPropertyChanged(); }
        }

        private string _workplaceError;
        public string WorkplaceError
        {
            get => _workplaceError;
            set { _workplaceError = value; OnPropertyChanged(); }
        }

        private string _monthlySalaryError;
        public string MonthlySalaryError
        {
            get => _monthlySalaryError;
            set { _monthlySalaryError = value; OnPropertyChanged(); }
        }

        private string _hourlyRateError;
        public string HourlyRateError
        {
            get => _hourlyRateError;
            set { _hourlyRateError = value; OnPropertyChanged(); }
        }

        private string _vacationDaysError;
        public string VacationDaysError
        {
            get => _vacationDaysError;
            set { _vacationDaysError = value; OnPropertyChanged(); }
        }
        #endregion

        #region Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        #region Methods
        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(JobTitle) &&
                   !string.IsNullOrWhiteSpace(Workplace);
        }

        private async void OnSave()
        {
            if (_isSaving)
                return;

            _isSaving = true;

            try
            {
                // Validering
                if (!Validate())
                    return;

                if (string.IsNullOrWhiteSpace(JobTitle) || string.IsNullOrWhiteSpace(Workplace))
                {
                    await Shell.Current.DisplayAlert(
                        Resources.Resx.Resources.Dialog_ErrorTitle,
                        Resources.Resx.Resources.Dialog_RequiredFieldsMessage,
                        Resources.Resx.Resources.Dialog_Ok);
                    return;
                }

                // Kolla befintliga jobb INNAN vi sparar
                var existingJobs = _databaseService.JobProfiles.GetJobProfiles();
                bool hasExistingJobs = existingJobs.Any();

                // Skapa JobProfile
                var jobProfile = new JobProfile
                {
                    JobTitle = JobTitle.Trim(),
                    Workplace = Workplace.Trim(),
                    EmploymentType = SelectedEmploymentType.Value,
                    PayPeriodType = PayPeriodType.CalendarMonth,
                    PayPeriodStartDay = 25,
                    TaxMethod = TaxCalculationMethod.Manual,
                    IsActive = !hasExistingJobs, // BARA första jobbet blir aktivt
                    Country = SelectedCountry.Country,

                    EmploymentStartDate = EmploymentStartDate,
                    VacationDaysPerYear = decimal.TryParse(VacationDaysPerYear, out var vacDays) ? vacDays : 25m,
                    InitialVacationBalance = decimal.TryParse(InitialVacationBalance, out var vacBalance) ? vacBalance : null
                };

                jobProfile.CurrencyCode = SelectedCurrency?.Value
                ?? SelectedCountry.Country.GetCurrencyCode()
                ?? "Euro";

                System.Diagnostics.Debug.WriteLine($"[SAVE] Saving job with CurrencyCode={jobProfile.CurrencyCode}");

                // Sätt lön
                if (IsMonthlySalary && decimal.TryParse(MonthlySalary, out var monthly))
                {
                    jobProfile.MonthlySalary = monthly;
                }
                else if (IsHourlySalary && decimal.TryParse(HourlyRate, out var hourly))
                {
                    jobProfile.HourlyRate = hourly;
                }

                // Sätt timmar och skatt
                if (decimal.TryParse(ExpectedHoursPerMonth, out var hours))
                {
                    jobProfile.ExpectedHoursPerMonth = hours;
                }

                if (decimal.TryParse(TaxRate, out var tax))
                {
                    jobProfile.ManualTaxRate = tax / 100;
                }

                // BARA inaktivera andra jobb om detta är första jobbet
                if (!hasExistingJobs)
                {
                    foreach (var job in existingJobs.Where(j => j.IsActive))
                    {
                        job.IsActive = false;
                        _databaseService.JobProfiles.SaveJobProfile(job);
                    }
                }

                // Spara nytt jobb
                _databaseService.JobProfiles.SaveJobProfile(jobProfile);

                // Hämta röda dagar för innevarande år
                await _holidayService.SyncFromApiAsync(jobProfile, DateTime.Now.Year);
                // Hämta röda dagar för nästa år också
                await _holidayService.SyncFromApiAsync(jobProfile, DateTime.Now.Year + 1);

                // Kolla resultat
                var allJobsAfter = _databaseService.JobProfiles.GetJobProfiles();

                await Shell.Current.DisplayAlert(
                    Resources.Resx.Resources.Dialog_SuccessTitle,
                    Resources.Resx.Resources.Dialog_JobSavedMessage,
                    Resources.Resx.Resources.Dialog_Ok);
                await Shell.Current.GoToAsync("//SettingsPage");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    Resources.Resx.Resources.Dialog_ErrorTitle,
                    string.Format(Resources.Resx.Resources.Dialog_JobSaveFailedMessageFormat, ex.Message),
                    Resources.Resx.Resources.Dialog_Ok);
            }
            finally
            {
                _isSaving = false;
            }
        }

        private async void OnCancel()
        {
            await Shell.Current.GoToAsync("..");
        }

        public void ClearForm()
        {
            JobTitle = string.Empty;
            Workplace = string.Empty;
            MonthlySalary = string.Empty;
            HourlyRate = string.Empty;
            ExpectedHoursPerMonth = "160";
            TaxRate = "33";
            SelectedEmploymentType = EmploymentTypes.FirstOrDefault(e => e.Value == EmploymentType.Permanent);
            SelectedSalaryType = SalaryTypes.FirstOrDefault(s => s.Value == "Monthly");

            EmploymentStartDate = DateTime.Today;
            VacationDaysPerYear = "25";
            InitialVacationBalance = string.Empty;
        }

        #endregion

        #region UI Validation
        private bool Validate()
        {
            bool isValid = true;

            // JobTitle
            if (string.IsNullOrWhiteSpace(JobTitle))
            {
                JobTitleError = Resources.Resx.Resources.Validation_JobTitleRequired;
                isValid = false;
            }
            else
                JobTitleError = string.Empty;

            // Workplace
            if (string.IsNullOrWhiteSpace(Workplace))
            {
                WorkplaceError = Resources.Resx.Resources.Validation_WorkplaceRequired;
                isValid = false;
            }
            else
                WorkplaceError = string.Empty;

            // Lön
            if (IsMonthlySalary && !decimal.TryParse(MonthlySalary, out _))
            {
                MonthlySalaryError = Resources.Resx.Resources.Validation_MonthlySalary;
                isValid = false;
            }
            else if (IsHourlySalary && !decimal.TryParse(HourlyRate, out _))
            {
                HourlyRateError = Resources.Resx.Resources.Validation_HourlyRate;
                isValid = false;
            }

            // Semester (endast för permanent)
            if (IsPermanentEmployment)
            {
                if (!decimal.TryParse(VacationDaysPerYear, out _))
                {
                    VacationDaysError = Resources.Resx.Resources.Validation_VacationDays;
                    isValid = false;
                }
                else
                    VacationDaysError = string.Empty;
            }

            return isValid;
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Employment & Salary Types

        // Egenskaper
        public ObservableCollection<LocalizedOption<EmploymentType>> EmploymentTypes { get; private set; }
        public ObservableCollection<LocalizedOption<string>> SalaryTypes { get; private set; }

        private LocalizedOption<EmploymentType> _selectedEmploymentType;
        public LocalizedOption<EmploymentType> SelectedEmploymentType
        {
            get => _selectedEmploymentType;
            set
            {
                if (_selectedEmploymentType != value)
                {
                    _selectedEmploymentType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsTemporaryEmployment));
                    OnPropertyChanged(nameof(IsPermanentEmployment));
                }
            }
        }

        private LocalizedOption<string> _selectedSalaryType;
        public LocalizedOption<string> SelectedSalaryType
        {
            get => _selectedSalaryType;
            set
            {
                if (_selectedSalaryType != value)
                {
                    _selectedSalaryType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsMonthlySalary));
                    OnPropertyChanged(nameof(IsHourlySalary));
                }
            }
        }

        // Hjälpegenskaper
        public bool IsMonthlySalary => SelectedSalaryType?.Value == "Monthly";
        public bool IsHourlySalary => SelectedSalaryType?.Value == "Hourly";

        public bool IsPermanentEmployment => SelectedEmploymentType?.Value == EmploymentType.Permanent;
        public bool IsTemporaryEmployment => SelectedEmploymentType?.Value == EmploymentType.Temporary;

        // Initiera listor (kallas från konstruktorn)
        private void InitializeEmploymentAndSalaryTypes()
        {
            EmploymentTypes = new ObservableCollection<LocalizedOption<EmploymentType>>
            {
                new LocalizedOption<EmploymentType>
                {
                    Value = EmploymentType.Permanent,
                    DisplayName = Resources.Resx.Resources.EmploymentType_Permanent
                },
                new LocalizedOption<EmploymentType>
                {
                    Value = EmploymentType.Temporary,
                    DisplayName = Resources.Resx.Resources.EmploymentType_Temporary
                }
            };

            SalaryTypes = new ObservableCollection<LocalizedOption<string>>
            {
                new LocalizedOption<string>
                {
                    Value = "Monthly",
                    DisplayName = Resources.Resx.Resources.SalaryType_Monthly
                },
                new LocalizedOption<string>
                {
                    Value = "Hourly",
                    DisplayName = Resources.Resx.Resources.SalaryType_Hourly
                }
            };

            // Standardval
            SelectedEmploymentType = EmploymentTypes.FirstOrDefault();
            SelectedSalaryType = SalaryTypes.FirstOrDefault();
        }

        // Uppdatera listorna vid språkbyte (om användaren byter språk i appen)
        public void RefreshLocalizedTexts()
        {
            foreach (var e in EmploymentTypes)
            {
                e.DisplayName = e.Value switch
                {
                    EmploymentType.Permanent => Resources.Resx.Resources.EmploymentType_Permanent,
                    EmploymentType.Temporary => Resources.Resx.Resources.EmploymentType_Temporary,
                    _ => e.DisplayName
                };
            }

            foreach (var s in SalaryTypes)
            {
                s.DisplayName = s.Value switch
                {
                    "Monthly" => Resources.Resx.Resources.SalaryType_Monthly,
                    "Hourly" => Resources.Resx.Resources.SalaryType_Hourly,
                    _ => s.DisplayName
                };
            }

            OnPropertyChanged(nameof(EmploymentTypes));
            OnPropertyChanged(nameof(SalaryTypes));
        }

        #endregion

        #region Currency Selection

        // Lista med tillgängliga valutor
        public ObservableCollection<LocalizedOption<string>> AvailableCurrencies { get; private set; }

        // Vald valuta
        private LocalizedOption<string> _selectedCurrency;
        public LocalizedOption<string> SelectedCurrency
        {
            get => _selectedCurrency;
            set
            {
                if (_selectedCurrency != value)
                {
                    _selectedCurrency = value;
                    OnPropertyChanged();
                }
            }
        }

        // Initiera valutalistan (kallas i konstruktorn)
        private void InitializeCurrencies()
        {
            var currencies = CurrencyHelper.GetAllCurrenciesLocalized();
            AvailableCurrencies = new ObservableCollection<LocalizedOption<string>>(currencies);

            SelectedCurrency = AvailableCurrencies.FirstOrDefault(c => c.Value == "SEK");
        }

        // Uppdatera visade texter om språket byts
        public void RefreshCurrencyTexts()
        {
            var currencies = CurrencyHelper.GetAllCurrenciesLocalized();
            AvailableCurrencies.Clear();
            foreach (var c in currencies)
                AvailableCurrencies.Add(c);
        }

        #endregion
    }
}
