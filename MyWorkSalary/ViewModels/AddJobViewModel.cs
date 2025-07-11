using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services;

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
        private string _expectedHoursPerMonth = "165";
        private string _taxRate = "33";
        private string _selectedEmploymentType = "Tillsvidare";
        private string _selectedSalaryType = "Månadslön";
        private bool _isSaving = false;

        private DateTime _employmentStartDate = DateTime.Today;
        private string _vacationDaysPerYear = "25";
        private string _initialVacationBalance = string.Empty;
        #endregion

        #region Constructor
        public AddJobViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;

            // Initiera listor
            EmploymentTypes = new ObservableCollection<string>
            {
                "Tillsvidare",          // → Permanent
                "Vikarie/Timanställd",  // → Temporary  
                "Behovsanställd"        // → OnCall
            };

            SalaryTypes = new ObservableCollection<string>
            {
                "Månadslön",
                "Timlön"
            };

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

        public ObservableCollection<string> EmploymentTypes { get; }
        public ObservableCollection<string> SalaryTypes { get; }

        public bool IsMonthlySalary => SelectedSalaryType == "Månadslön";
        public bool IsHourlySalary => SelectedSalaryType == "Timlön";

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
            {
                System.Diagnostics.Debug.WriteLine("REDAN SPARAR - hoppar över");
                return;
            }

            _isSaving = true;

            try
            {
                // Validering
                if (string.IsNullOrWhiteSpace(JobTitle) || string.IsNullOrWhiteSpace(Workplace))
                {
                    await Shell.Current.DisplayAlert("Fel", "Fyll i alla obligatoriska fält", "OK");
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
                    EmploymentType = ParseEmploymentType(SelectedEmploymentType),
                    PayPeriodType = PayPeriodType.CalendarMonth,
                    PayPeriodStartDay = 25,
                    TaxMethod = TaxCalculationMethod.Manual,
                    IsActive = !hasExistingJobs, // BARA första jobbet blir aktivt

                    EmploymentStartDate = EmploymentStartDate,
                    VacationDaysPerYear = decimal.TryParse(VacationDaysPerYear, out var vacDays) ? vacDays : 25m,
                    InitialVacationBalance = decimal.TryParse(InitialVacationBalance, out var vacBalance) ? vacBalance : null
                };

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

                // Kolla resultat
                var allJobsAfter = _databaseService.JobProfiles.GetJobProfiles();

                await Shell.Current.DisplayAlert("Framgång", "Jobbet har sparats!", "OK");
                await Shell.Current.GoToAsync("//SettingsPage");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fel", $"Kunde inte spara jobbet: {ex.Message}", "OK");
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
            ExpectedHoursPerMonth = "165";
            TaxRate = "33";
            SelectedEmploymentType = "Tillsvidare";
            SelectedSalaryType = "Månadslön";

            EmploymentStartDate = DateTime.Today;
            VacationDaysPerYear = "25";
            InitialVacationBalance = string.Empty;
        }

        private EmploymentType ParseEmploymentType(string type)
        {
            return type switch
            {
                "Tillsvidare" => EmploymentType.Permanent,
                "Vikarie/Timanställd" => EmploymentType.Temporary,
                "Behovsanställd" => EmploymentType.OnCall,
                _ => EmploymentType.Permanent
            };
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
