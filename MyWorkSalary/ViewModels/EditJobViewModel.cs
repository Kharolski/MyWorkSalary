using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public class EditJobViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly DatabaseService _databaseService;
        private JobProfile _originalJob;        // Orörd original från databas
        private JobProfile _workingCopy;       // Arbetskopia som vi ändrar
        private string _jobTitle;
        private string _workplace;
        private string _selectedEmploymentType;
        private string _selectedSalaryType;
        private string _monthlySalary;
        private string _hourlyRate;
        private string _expectedHoursPerMonth;
        private string _taxRate;
        private DateTime _employmentStartDate = DateTime.Today;
        private string _vacationDaysPerYear = "25";
        private string _initialVacationBalance = string.Empty;
        #endregion

        #region Constructor
        public EditJobViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;

            EmploymentTypes = new ObservableCollection<string>
            {
                Resources.Resx.Resources.EmploymentType_Permanent,
                Resources.Resx.Resources.EmploymentType_Temporary
            };

            SalaryTypes = new ObservableCollection<string>
            {
                Resources.Resx.Resources.SalaryType_Monthly,
                Resources.Resx.Resources.SalaryType_Hourly
            };

            AvailableCurrencies = new ObservableCollection<LocalizedOption<string>>(CurrencyHelper.GetAllCurrenciesLocalized());

            SaveCommand = new Command(OnSave, CanSave);
            CancelCommand = new Command(OnCancel);

            PropertyChanged += (s, e) => ((Command)SaveCommand).ChangeCanExecute();
        }
        #endregion

        #region Properties
        private SupportedCountry _country;
        public SupportedCountry Country
        {
            get => _country;
            set
            {
                if (_country != value)
                {
                    _country = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CountryDisplayName));
                }
            }
        }

        private string _currencyCode;
        public string CurrencyCode
        {
            get => _currencyCode;
            set
            {
                if (_currencyCode != value)
                {
                    _currencyCode = value;
                    OnPropertyChanged();
                }
            }
        }

        private LocalizedOption<string> _selectedCurrency;
        public ObservableCollection<LocalizedOption<string>> AvailableCurrencies { get; private set; }

        public LocalizedOption<string> SelectedCurrency
        {
            get => _selectedCurrency;
            set
            {
                if (_selectedCurrency != value)
                {
                    _selectedCurrency = value;
                    if (value != null)
                        CurrencyCode = value.Value;
                    OnPropertyChanged();
                }
            }
        }

        public string CountryDisplayName => Country.GetDisplayName();

        public ObservableCollection<string> EmploymentTypes { get; }
        public ObservableCollection<string> SalaryTypes { get; }

        public string JobTitle
        {
            get => _jobTitle;
            set
            {
                _jobTitle = value;
                OnPropertyChanged();
            }
        }

        public string Workplace
        {
            get => _workplace;
            set
            {
                _workplace = value;
                OnPropertyChanged();
            }
        }

        public string SelectedEmploymentType
        {
            get => _selectedEmploymentType;
            set
            {
                _selectedEmploymentType = value;
                OnPropertyChanged();
            }
        }

        public string SelectedSalaryType
        {
            get => _selectedSalaryType;
            set
            {
                _selectedSalaryType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMonthlySalary));
                OnPropertyChanged(nameof(IsHourlySalary));
            }
        }

        public string MonthlySalary
        {
            get => _monthlySalary;
            set
            {
                _monthlySalary = value;
                OnPropertyChanged();
            }
        }

        public string HourlyRate
        {
            get => _hourlyRate;
            set
            {
                _hourlyRate = value;
                OnPropertyChanged();
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

        public DateTime EmploymentStartDate
        {
            get => _employmentStartDate;
            set
            {
                _employmentStartDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowInitialVacationBalance));
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

        public bool ShowInitialVacationBalance =>
            DateTime.Today.Subtract(EmploymentStartDate).TotalDays > 30;

        public bool IsMonthlySalary => SelectedSalaryType == Resources.Resx.Resources.SalaryType_Monthly;
        public bool IsHourlySalary => SelectedSalaryType == Resources.Resx.Resources.SalaryType_Hourly;
        #endregion

        #region Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        #region Methods
        public void LoadJob(int jobId)
        {
            // Hämta original från databas (denna förblir orörd)
            _originalJob = _databaseService.JobProfiles.GetJobProfile(jobId);

            if (_originalJob != null)
            {
                // Skapa arbetskopia - DJUP KOPIA av alla värden
                _workingCopy = CreateWorkingCopy(_originalJob);

                // Fyll formuläret från arbetskopian
                PopulateFormFromWorkingCopy();

                System.Diagnostics.Debug.WriteLine($"[LOAD] Loaded job with CurrencyCode={_workingCopy.CurrencyCode}");
            }
        }

        private JobProfile CreateWorkingCopy(JobProfile original)
        {
            return new JobProfile
            {
                Id = original.Id,
                JobTitle = original.JobTitle,
                Workplace = original.Workplace,
                EmploymentType = original.EmploymentType,
                EmploymentStartDate = original.EmploymentStartDate,
                MonthlySalary = original.MonthlySalary,
                HourlyRate = original.HourlyRate,
                ExpectedHoursPerMonth = original.ExpectedHoursPerMonth,
                ManualTaxRate = original.ManualTaxRate,
                VacationDaysPerYear = original.VacationDaysPerYear,
                InitialVacationBalance = original.InitialVacationBalance,
                PayPeriodType = original.PayPeriodType,
                PayPeriodStartDay = original.PayPeriodStartDay,
                TaxMethod = original.TaxMethod,
                IsActive = original.IsActive,
                CreatedDate = original.CreatedDate,
                ModifiedDate = original.ModifiedDate,

                CurrencyCode = original.CurrencyCode,
                Country = original.Country
            };
        }

        private void PopulateFormFromWorkingCopy()
        {
            JobTitle = _workingCopy.JobTitle;
            Workplace = _workingCopy.Workplace;
            SelectedEmploymentType = GetEmploymentTypeString(_workingCopy.EmploymentType);
            EmploymentStartDate = _workingCopy.EmploymentStartDate;
            ExpectedHoursPerMonth = _workingCopy.ExpectedHoursPerMonth.ToString();
            TaxRate = (_workingCopy.ManualTaxRate * 100).ToString();
            VacationDaysPerYear = _workingCopy.VacationDaysPerYear.ToString();
            InitialVacationBalance = _workingCopy.InitialVacationBalance?.ToString() ?? string.Empty;

            Country = _workingCopy.Country;

            // Säkerställ att AvailableCurrencies är fylld
            if (AvailableCurrencies == null || !AvailableCurrencies.Any())
                AvailableCurrencies = new ObservableCollection<LocalizedOption<string>>(CurrencyHelper.GetAllCurrenciesLocalized());

            // Sätt valutan korrekt
            var currency = AvailableCurrencies.FirstOrDefault(c => c.Value == _workingCopy.CurrencyCode);
            if (currency != null)
                SelectedCurrency = currency;
            else
                SelectedCurrency = AvailableCurrencies.FirstOrDefault(c => c.Value == "SEK");

            // Sätt lönetyp och värden
            if (_workingCopy.MonthlySalary > 0)
            {
                SelectedSalaryType = Resources.Resx.Resources.SalaryType_Monthly;
                MonthlySalary = _workingCopy.MonthlySalary.ToString();
                HourlyRate = string.Empty; // Rensa timlön i formuläret
            }
            else
            {
                SelectedSalaryType = Resources.Resx.Resources.SalaryType_Hourly;
                HourlyRate = _workingCopy.HourlyRate.ToString();
                MonthlySalary = string.Empty; // Rensa månadslön i formuläret
            }
        }

        public void RefreshCurrencyTexts()
        {
            var currencies = CurrencyHelper.GetAllCurrenciesLocalized();
            AvailableCurrencies.Clear();
            foreach (var c in currencies)
                AvailableCurrencies.Add(c);
        }

        private string GetEmploymentTypeString(EmploymentType employmentType)
        {
            return employmentType switch
            {
                EmploymentType.Permanent => Resources.Resx.Resources.EmploymentType_Permanent,
                EmploymentType.Temporary => Resources.Resx.Resources.EmploymentType_Temporary,
                _ => Resources.Resx.Resources.EmploymentType_Permanent
            };
        }

        private EmploymentType ParseEmploymentType(string type)
        {
            if (type == Resources.Resx.Resources.EmploymentType_Permanent)
                return EmploymentType.Permanent;
            if (type == Resources.Resx.Resources.EmploymentType_Temporary)
                return EmploymentType.Temporary;

            return EmploymentType.Permanent;
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(JobTitle) &&
                   !string.IsNullOrWhiteSpace(Workplace) &&
                   !string.IsNullOrWhiteSpace(SelectedEmploymentType) &&
                   !string.IsNullOrWhiteSpace(SelectedSalaryType) &&
                   !string.IsNullOrWhiteSpace(ExpectedHoursPerMonth) &&
                   !string.IsNullOrWhiteSpace(TaxRate) &&
                   (IsMonthlySalary ? !string.IsNullOrWhiteSpace(MonthlySalary) : !string.IsNullOrWhiteSpace(HourlyRate));
        }

        private async void OnSave()
        {
            try
            {
                if (_originalJob == null)
                    return;

                // Uppdatera ORIGINAL med formulärdata (inte arbetskopian)
                _originalJob.JobTitle = JobTitle.Trim();
                _originalJob.Workplace = Workplace.Trim();
                _originalJob.EmploymentType = ParseEmploymentType(SelectedEmploymentType);
                _originalJob.EmploymentStartDate = EmploymentStartDate;
                _originalJob.VacationDaysPerYear = decimal.TryParse(VacationDaysPerYear, out var vacDays) ? vacDays : 25m;
                _originalJob.InitialVacationBalance = decimal.TryParse(InitialVacationBalance, out var vacBalance) ? vacBalance : null;

                _originalJob.CurrencyCode = CurrencyCode;
                _originalJob.Country = Country;

                // Uppdatera lön
                if (IsMonthlySalary && decimal.TryParse(MonthlySalary, out var monthly))
                {
                    _originalJob.MonthlySalary = monthly;
                    _originalJob.HourlyRate = 0; // Rensa timlön
                }
                else if (IsHourlySalary && decimal.TryParse(HourlyRate, out var hourly))
                {
                    _originalJob.HourlyRate = hourly;
                    _originalJob.MonthlySalary = 0; // Rensa månadslön
                }

                // Uppdatera timmar och skatt
                if (decimal.TryParse(ExpectedHoursPerMonth, out var hours))
                {
                    _originalJob.ExpectedHoursPerMonth = hours;
                }

                if (decimal.TryParse(TaxRate, out var tax))
                {
                    _originalJob.ManualTaxRate = tax / 100;
                }

                // Spara ändringar till databas
                _databaseService.JobProfiles.SaveJobProfile(_originalJob);

                await Shell.Current.DisplayAlert(
                    Resources.Resx.Resources.EditJob_SuccessTitle,
                    Resources.Resx.Resources.EditJob_SuccessMessage,
                    Resources.Resx.Resources.EditJob_Ok);

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    Resources.Resx.Resources.EditJob_ErrorTitle,
                    string.Format(Resources.Resx.Resources.EditJob_ErrorMessage, ex.Message),
                    Resources.Resx.Resources.EditJob_Ok);
            }
        }

        private async void OnCancel()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                Resources.Resx.Resources.EditJob_CancelTitle,
                Resources.Resx.Resources.EditJob_CancelMessage,
                Resources.Resx.Resources.EditJob_CancelYes,
                Resources.Resx.Resources.EditJob_CancelNo);

            if (confirm)
            {
                // Inget behöver återställas - _originalJob är orörd!
                await Shell.Current.GoToAsync("..");
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
