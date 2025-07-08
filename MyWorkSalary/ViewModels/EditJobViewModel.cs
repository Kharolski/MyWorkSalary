using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services;

namespace MyWorkSalary.ViewModels
{
    public class EditJobViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly DatabaseService _databaseService;
        private JobProfile _originalJob;
        private string _jobTitle;
        private string _workplace;
        private string _selectedEmploymentType;
        private string _selectedSalaryType;
        private string _monthlySalary;
        private string _hourlyRate;
        private string _expectedHoursPerMonth;
        private string _taxRate;
        #endregion

        #region Constructor
        public EditJobViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;

            // Initiera listor (samma som AddJobViewModel)
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

        public bool IsMonthlySalary => SelectedSalaryType == "Månadslön";
        public bool IsHourlySalary => SelectedSalaryType == "Timlön";
        #endregion

        #region Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        #region Methods
        public void LoadJob(int jobId)
        {
            var jobs = _databaseService.JobProfiles.GetJobProfiles();
            _originalJob = jobs.FirstOrDefault(j => j.Id == jobId);

            if (_originalJob != null)
            {
                // Fyll i formuläret med befintlig data
                JobTitle = _originalJob.JobTitle;
                Workplace = _originalJob.Workplace;
                SelectedEmploymentType = GetEmploymentTypeString(_originalJob.EmploymentType);
                ExpectedHoursPerMonth = _originalJob.ExpectedHoursPerMonth.ToString();
                TaxRate = (_originalJob.ManualTaxRate * 100).ToString(); // Konvertera tillbaka till %

                // Sätt lönetyp och värden
                if (_originalJob.MonthlySalary > 0)
                {
                    SelectedSalaryType = "Månadslön";
                    MonthlySalary = _originalJob.MonthlySalary.ToString();
                }
                else
                {
                    SelectedSalaryType = "Timlön";
                    HourlyRate = _originalJob.HourlyRate.ToString();
                }
            }
        }

        private string GetEmploymentTypeString(EmploymentType employmentType)
        {
            return employmentType switch
            {
                EmploymentType.Permanent => "Tillsvidare",
                EmploymentType.Temporary => "Vikarie/Timanställd",
                EmploymentType.OnCall => "Behovsanställd",
                _ => "Tillsvidare"
            };
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

                // Uppdatera befintligt jobb
                _originalJob.JobTitle = JobTitle.Trim();
                _originalJob.Workplace = Workplace.Trim();
                _originalJob.EmploymentType = ParseEmploymentType(SelectedEmploymentType);

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
                    _originalJob.ManualTaxRate = tax / 100; // Konvertera % till decimal
                }

                // Spara ändringar
                _databaseService.JobProfiles.SaveJobProfile(_originalJob);

                await Shell.Current.DisplayAlert("Framgång", "Jobbet har uppdaterats!", "OK");

                // Tillbaka till Settings
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fel", $"Kunde inte uppdatera jobbet: {ex.Message}", "OK");
            }
        }

        private async void OnCancel()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Avbryt",
                "Är du säker på att du vill avbryta? Ändringar kommer inte att sparas.",
                "Ja, avbryt",
                "Nej, fortsätt");

            if (confirm)
            {
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
