using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.ViewModels
{
    public class AddOBRateViewModel : INotifyPropertyChanged
    {
        #region Fields
        private readonly DatabaseService _databaseService;
        private readonly JobProfile _activeJob;
        private readonly IOBEventService _obEventService;

        // Properties för formuläret
        private string _name = string.Empty;
        private TimeSpan _startTime = new TimeSpan(18, 0, 0); // Default 18:00
        private TimeSpan _endTime = new TimeSpan(22, 0, 0);   // Default 22:00
        private string _ratePerHour = string.Empty;
        private string _selectedCategory = Resources.Resx.Resources.Category_Placeholder; // default text

        // Veckodagar
        private bool _monday;
        private bool _tuesday;
        private bool _wednesday;
        private bool _thursday;
        private bool _friday;
        private bool _saturday;
        private bool _sunday;
        private bool _holidays;
        #endregion

        #region Constructor
        public AddOBRateViewModel(DatabaseService databaseService, IOBEventService obEventService)
        {
            _databaseService = databaseService;
            _activeJob = _databaseService.JobProfiles.GetActiveJob();
            _obEventService = obEventService;

            // Initiera commands
            SaveCommand = new Command(OnSave);
            SelectWeekdaysCommand = new Command(OnSelectWeekdays);
            SelectWeekendsCommand = new Command(OnSelectWeekends);
            SelectAllDaysCommand = new Command(OnSelectAllDays);

            // Initiera kategorier
            Categories = new ObservableCollection<string> 
            {
                Resources.Resx.Resources.Category_Placeholder,
                Resources.Resx.Resources.Category_Evening,
                Resources.Resx.Resources.Category_Night,
                Resources.Resx.Resources.Category_Weekend,
                Resources.Resx.Resources.Category_WeekendExtra,
                Resources.Resx.Resources.Category_Holiday
            };
        }
        #endregion

        #region Properties
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    ValidateNotEmpty(value, msg => NameError = msg, Resources.Resx.Resources.Validation_OBNameRequired);
                }
            }
        }

        public TimeSpan StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public TimeSpan EndTime
        {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        public string RatePerHour
        {
            get => _ratePerHour;
            set
            {
                if (SetProperty(ref _ratePerHour, value))
                {
                    ValidateNotEmpty(value, msg => RateError = msg, Resources.Resx.Resources.Validation_OBRateRequired);
                }
            }
        }

        public ObservableCollection<string> Categories { get; }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    // Validation: placeholder eller tom = fel
                    CategoryError = string.IsNullOrWhiteSpace(value) || value == Resources.Resx.Resources.Category_Placeholder
                        ? Resources.Resx.Resources.Validation_OBCategoryRequired
                        : string.Empty;
                }
            }
        }
        #endregion

        #region Validation Properties
        private string _nameError;
        public string NameError
        {
            get => _nameError;
            set => SetProperty(ref _nameError, value);
        }

        private string _rateError;
        public string RateError
        {
            get => _rateError;
            set => SetProperty(ref _rateError, value);
        }

        private string _categoryError;
        public string CategoryError
        {
            get => _categoryError;
            set => SetProperty(ref _categoryError, value);
        }

        private string _daysError;
        public string DaysError
        {
            get => _daysError;
            set => SetProperty(ref _daysError, value);
        }
        #endregion

        #region Weekday Properties
        public bool Monday
        {
            get => _monday;
            set
            {
                if (SetProperty(ref _monday, value))
                    ValidateDays();
            }
        }

        public bool Tuesday
        {
            get => _tuesday;
            set
            {
                if (SetProperty(ref _tuesday, value))
                    ValidateDays();
            }
        }

        public bool Wednesday
        {
            get => _wednesday;
            set
            {
                if (SetProperty(ref _wednesday, value))
                    ValidateDays();
            }
        }

        public bool Thursday
        {
            get => _thursday;
            set
            {
                if (SetProperty(ref _thursday, value))
                    ValidateDays();
            }
        }

        public bool Friday
        {
            get => _friday;
            set
            {
                if (SetProperty(ref _friday, value))
                    ValidateDays();
            }
        }

        public bool Saturday
        {
            get => _saturday;
            set
            {
                if (SetProperty(ref _saturday, value))
                    ValidateDays();
            }
        }

        public bool Sunday
        {
            get => _sunday;
            set
            {
                if (SetProperty(ref _sunday, value))
                    ValidateDays();
            }
        }

        public bool Holidays
        {
            get => _holidays;
            set
            {
                if (SetProperty(ref _holidays, value))
                    ValidateDays();
            }
        }
        #endregion

        #region Commands
        public ICommand SaveCommand { get; }
        public ICommand SelectWeekdaysCommand { get; }
        public ICommand SelectWeekendsCommand { get; }
        public ICommand SelectAllDaysCommand { get; }
        #endregion

        #region Command Methods
        private async void OnSave()
        {
            if (!IsValid())
                return;

            try
            {
                if (!decimal.TryParse(RatePerHour, out decimal rate))
                {
                    await Shell.Current.DisplayAlert(
                        Resources.Resx.Resources.Save_Error_Title,
                        Resources.Resx.Resources.Validation_OBRateRequired,
                        "OK");
                    return;
                }

                var obRate = new OBRate
                {
                    JobProfileId = _activeJob.Id,
                    Name = Name,
                    StartTime = StartTime,
                    EndTime = EndTime,
                    RatePerHour = rate,
                    Category = ParseCategory(SelectedCategory),
                    Monday = Monday,
                    Tuesday = Tuesday,
                    Wednesday = Wednesday,
                    Thursday = Thursday,
                    Friday = Friday,
                    Saturday = Saturday,
                    Sunday = Sunday,
                    Holidays = Holidays,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                _databaseService.OBRates.SaveOBRate(obRate);

                // REBUILD 4 månader bakåt (bara om det finns aktivt jobb)
                if (_activeJob != null)
                    await _obEventService.RebuildForJobLastMonths(_activeJob.Id, 4);

                await Shell.Current.DisplayAlert(
                    Resources.Resx.Resources.Save_Success_Title,
                    string.Format(Resources.Resx.Resources.Save_Success_Message, Name),
                    "OK");

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    Resources.Resx.Resources.Save_Error_Title,
                    string.Format(Resources.Resx.Resources.Save_Error_Message, ex.Message),
                    "OK");
            }
        }

        private void OnSelectWeekdays()
        {
            Monday = Tuesday = Wednesday = Thursday = Friday = true;
            Saturday = Sunday = Holidays = false;
        }

        private void OnSelectWeekends()
        {
            Saturday = Sunday = true;
            Monday = Tuesday = Wednesday = Thursday = Friday = Holidays = false;
        }

        private void OnSelectAllDays()
        {
            Monday = Tuesday = Wednesday = Thursday = Friday = Saturday = Sunday = Holidays = true;
        }
        #endregion

        #region Validation
        private bool IsValid()
        {
            bool isValid = true;

            // Namn
            if (string.IsNullOrWhiteSpace(Name))
            {
                NameError = Resources.Resx.Resources.Validation_OBNameRequired;
                isValid = false;
            }
            else
            {
                NameError = string.Empty;
            }

            // Lön
            if (string.IsNullOrWhiteSpace(RatePerHour) || !decimal.TryParse(RatePerHour, out _))
            {
                RateError = Resources.Resx.Resources.Validation_OBRateRequired;
                isValid = false;
            }
            else
            {
                RateError = string.Empty;
            }

            // Kategori
            if (string.IsNullOrWhiteSpace(SelectedCategory) || SelectedCategory == Resources.Resx.Resources.Category_Placeholder)
            {
                CategoryError = Resources.Resx.Resources.Validation_OBCategoryRequired;
                isValid = false;
            }
            else
            {
                CategoryError = string.Empty;
            }

            // Minst en dag vald
            if (!Monday && !Tuesday && !Wednesday && !Thursday && !Friday && !Saturday && !Sunday && !Holidays)
            {
                DaysError = Resources.Resx.Resources.Validation_OBDaysRequired;
                isValid = false;
            }
            else
            {
                DaysError = string.Empty;
            }

            return isValid;
        }
        #endregion

        #region Helpers
        private void ValidateNotEmpty(string value, Action<string> setErrorAction, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
                setErrorAction(errorMessage);
            else
                setErrorAction(string.Empty);
        }

        private void ValidateDays()
        {
            if (!Monday && !Tuesday && !Wednesday && !Thursday && !Friday && !Saturday && !Sunday && !Holidays)
                DaysError = Resources.Resx.Resources.Validation_OBDaysRequired;
            else
                DaysError = string.Empty;
        }

        private OBCategory ParseCategory(string category)
        {
            return category switch
            {
                var c when c == Resources.Resx.Resources.Category_Evening => OBCategory.Evening,
                var c when c == Resources.Resx.Resources.Category_Night => OBCategory.Night,
                var c when c == Resources.Resx.Resources.Category_Weekend => OBCategory.Weekend,
                var c when c == Resources.Resx.Resources.Category_WeekendExtra => OBCategory.WeekendExtra,
                var c when c == Resources.Resx.Resources.Category_Holiday => OBCategory.Holiday,
                _ => OBCategory.Evening // default fallback (men borde aldrig hända)
            };
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}
