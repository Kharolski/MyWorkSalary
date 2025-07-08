using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services;

namespace MyWorkSalary.ViewModels
{
    public class AddOBRateViewModel : INotifyPropertyChanged
    {
        #region Fields
        private readonly DatabaseService _databaseService;
        private readonly JobProfile _activeJob;

        // Properties för formuläret
        private string _name = string.Empty;
        private TimeSpan _startTime = new TimeSpan(18, 0, 0); // Default 18:00
        private TimeSpan _endTime = new TimeSpan(22, 0, 0);   // Default 22:00
        private string _ratePerHour = string.Empty;
        private OBCategory _selectedCategory = OBCategory.Evening;

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
        public AddOBRateViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _activeJob = _databaseService.JobProfiles.GetActiveJob();

            // Initiera commands
            SaveCommand = new Command(OnSave);
            SelectWeekdaysCommand = new Command(OnSelectWeekdays);
            SelectWeekendsCommand = new Command(OnSelectWeekends);
            SelectAllDaysCommand = new Command(OnSelectAllDays);

            // Initiera kategorier
            Categories = new ObservableCollection<string>
            {
                "Kväll",
                "Natt",
                "Helg",
                "Helg Extra",
                "Helgdag"
            };
        }
        #endregion

        #region Properties
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
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
            set => SetProperty(ref _ratePerHour, value);
        }

        public ObservableCollection<string> Categories { get; }

        public string SelectedCategory
        {
            get => _selectedCategory.ToString();
            set
            {
                if (Enum.TryParse<OBCategory>(value, out var category))
                {
                    SetProperty(ref _selectedCategory, category);
                }
            }
        }
        #endregion

        #region Weekday Properties
        public bool Monday
        {
            get => _monday;
            set => SetProperty(ref _monday, value);
        }

        public bool Tuesday
        {
            get => _tuesday;
            set => SetProperty(ref _tuesday, value);
        }

        public bool Wednesday
        {
            get => _wednesday;
            set => SetProperty(ref _wednesday, value);
        }

        public bool Thursday
        {
            get => _thursday;
            set => SetProperty(ref _thursday, value);
        }

        public bool Friday
        {
            get => _friday;
            set => SetProperty(ref _friday, value);
        }

        public bool Saturday
        {
            get => _saturday;
            set => SetProperty(ref _saturday, value);
        }

        public bool Sunday
        {
            get => _sunday;
            set => SetProperty(ref _sunday, value);
        }

        public bool Holidays
        {
            get => _holidays;
            set => SetProperty(ref _holidays, value);
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
                    await Shell.Current.DisplayAlert("Fel", "Ange en giltig lön per timme", "OK");
                    return;
                }

                var obRate = new OBRate
                {
                    JobProfileId = _activeJob.Id,
                    Name = Name,
                    StartTime = StartTime,
                    EndTime = EndTime,
                    RatePerHour = rate,
                    Category = _selectedCategory,
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

                await Shell.Current.DisplayAlert(
                    "Sparat!",
                    $"OB-regel '{Name}' har sparats.",
                    "OK");

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Fel",
                    $"Kunde inte spara OB-regel: {ex.Message}",
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
            if (string.IsNullOrWhiteSpace(Name))
            {
                Shell.Current.DisplayAlert("Fel", "Ange ett namn för OB-regeln", "OK");
                return false;
            }

            if (string.IsNullOrWhiteSpace(RatePerHour))
            {
                Shell.Current.DisplayAlert("Fel", "Ange lön per timme", "OK");
                return false;
            }

            if (!Monday && !Tuesday && !Wednesday && !Thursday && !Friday && !Saturday && !Sunday && !Holidays)
            {
                Shell.Current.DisplayAlert("Fel", "Välj minst en dag som regeln gäller för", "OK");
                return false;
            }

            return true;
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
