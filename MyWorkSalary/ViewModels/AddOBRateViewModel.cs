using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services;
using MyWorkSalary.Services.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public class AddOBRateViewModel : BaseViewModel
    {
        #region Fields
        private readonly DatabaseService _databaseService;
        private readonly JobProfile _activeJob;
        private readonly IOBEventService _obEventService;
        private int _editingOBRateId = 0;

        // för formuläret
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
        private bool _bigHolidays;
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

        #region Edit/Create Initialization

        public void PrepareForCreate()
        {
            _editingOBRateId = 0;

            // återställ feltexter
            NameError = RateError = CategoryError = DaysError = string.Empty;

        }

        public void LoadForEdit(int obRateId)
        {
            var ob = _databaseService.OBRates.GetOBRate(obRateId);
            if (ob == null)
                return;

            _editingOBRateId = ob.Id;

            Name = ob.Name;
            StartTime = ob.StartTime;
            EndTime = ob.EndTime;
            RatePerHour = ob.RatePerHour.ToString();

            // Category -> SelectedCategory text
            SelectedCategory = CategoryToText(ob.Category);

            Monday = ob.Monday;
            Tuesday = ob.Tuesday;
            Wednesday = ob.Wednesday;
            Thursday = ob.Thursday;
            Friday = ob.Friday;
            Saturday = ob.Saturday;
            Sunday = ob.Sunday;
            Holidays = ob.Holidays;
            BigHolidays = ob.BigHolidays;

            // Nollställ fel
            NameError = RateError = CategoryError = DaysError = string.Empty;
        }

        private string CategoryToText(OBCategory category)
        {
            return category switch
            {
                OBCategory.Evening => Resources.Resx.Resources.Category_Evening,
                OBCategory.Night => Resources.Resx.Resources.Category_Night,
                OBCategory.Weekend => Resources.Resx.Resources.Category_Weekend,
                OBCategory.WeekendExtra => Resources.Resx.Resources.Category_WeekendExtra,
                OBCategory.Holiday => Resources.Resx.Resources.Category_Holiday,
                _ => Resources.Resx.Resources.Category_Placeholder
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
                var normalized = NormalizeDecimalInput(value);

                if (SetProperty(ref _ratePerHour, normalized))
                {
                    ValidateNotEmpty(normalized, msg => RateError = msg, Resources.Resx.Resources.Validation_OBRateRequired);
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
        public bool BigHolidays
        {
            get => _bigHolidays;
            set
            {
                if (SetProperty(ref _bigHolidays, value))
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
                if (!TryParseRate(RatePerHour, out decimal rate))
                {
                    await Shell.Current.DisplayAlert(
                        Resources.Resx.Resources.Save_Error_Title,
                        Resources.Resx.Resources.Validation_OBRateRequired,
                        "OK");
                    return;
                }

                var category = ParseCategory(SelectedCategory);
                var obRate = new OBRate
                {
                    Id = _editingOBRateId,
                    JobProfileId = _activeJob.Id,
                    Name = Name,
                    StartTime = StartTime,
                    EndTime = EndTime,
                    RatePerHour = rate,
                    Category = category,

                    Priority = GetDefaultPriority(category, BigHolidays),
                    Monday = Monday,
                    Tuesday = Tuesday,
                    Wednesday = Wednesday,
                    Thursday = Thursday,
                    Friday = Friday,
                    Saturday = Saturday,
                    Sunday = Sunday,
                    Holidays = Holidays,
                    BigHolidays = BigHolidays,

                    IsActive = true
                };

                // CreatedDate: bara om ny post 
                if (_editingOBRateId == 0)
                    obRate.CreatedDate = DateTime.Now;

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
            if (!Monday && !Tuesday && !Wednesday && !Thursday && !Friday && !Saturday && !Sunday && !Holidays && !BigHolidays)
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
            if (!Monday && !Tuesday && !Wednesday && !Thursday && !Friday && !Saturday && !Sunday && !Holidays && !BigHolidays)
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

        private int GetDefaultPriority(OBCategory category, bool bigHoliday)
        {
            if (bigHoliday)
                return 60;

            return category switch
            {
                OBCategory.Evening => 10,
                OBCategory.Night => 20,
                OBCategory.Weekend => 30,
                OBCategory.WeekendExtra => 40,
                OBCategory.Holiday => 50,
                _ => 0
            };
        }

        private bool TryParseRate(string input, out decimal rate)
        {
            return decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out rate);
        }

        private string NormalizeDecimalInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // tillåt bara siffror + en decimal-separator
            var decSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator; // "," eller "."
            var altSep = decSep == "," ? "." : ",";

            input = input.Trim();

            // byt alternativ separator till aktuell
            input = input.Replace(altSep, decSep);

            // filtrera bort allt annat än siffror och decimal-separator
            var sb = new StringBuilder();
            bool hasSep = false;

            foreach (var ch in input)
            {
                if (char.IsDigit(ch))
                {
                    sb.Append(ch);
                }
                else if (!hasSep && ch.ToString() == decSep)
                {
                    sb.Append(decSep);
                    hasSep = true;
                }
                // ignorera övriga tecken
            }

            return sb.ToString();
        }
        #endregion
    }
}
