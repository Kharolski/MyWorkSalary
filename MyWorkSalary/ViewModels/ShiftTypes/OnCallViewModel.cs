using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Services.Handlers;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyWorkSalary.ViewModels.ShiftTypes
{
    public class OnCallViewModel : INotifyPropertyChanged
    {
        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly IShiftValidationService _validationService;
        private readonly OnCallHandler _onCallHandler;

        private DateTime _selectedDate;
        private JobProfile _activeJob;
        private TimeSpan _standbyStartTime = new TimeSpan(18, 0, 0); // 18:00
        private TimeSpan _standbyEndTime = new TimeSpan(8, 0, 0);    // 08:00
        private string _activeHours = "0";
        private string _onCallRate = "40";
        private string _notes = "";
        private string _validationMessage = "";

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action ValidationChanged;

        #region Constructor

        public OnCallViewModel(
            IWorkShiftRepository workShiftRepository,
            IShiftValidationService validationService,
            OnCallHandler onCallHandler)
        {
            _workShiftRepository = workShiftRepository;
            _validationService = validationService;
            _onCallHandler = onCallHandler;
        }

        #endregion

        #region Properties

        public TimeSpan StandbyStartTime
        {
            get => _standbyStartTime;
            set
            {
                _standbyStartTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CalculationSummary));
                OnPropertyChanged(nameof(StandbyHoursText));
                OnPropertyChanged(nameof(CalculatedPay));
                ValidateInput();
                ValidationChanged?.Invoke();
            }
        }

        public TimeSpan StandbyEndTime
        {
            get => _standbyEndTime;
            set
            {
                _standbyEndTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CalculationSummary));
                OnPropertyChanged(nameof(StandbyHoursText));
                OnPropertyChanged(nameof(CalculatedPay));
                ValidateInput();
                ValidationChanged?.Invoke();
            }
        }

        public string ActiveHours
        {
            get => _activeHours;
            set
            {
                _activeHours = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CalculationSummary));
                OnPropertyChanged(nameof(CalculatedPay));
                OnPropertyChanged(nameof(ShowActiveHoursInfo));
                ValidateInput();
                ValidationChanged?.Invoke();
            }
        }

        public string OnCallRate
        {
            get => _onCallRate;
            set
            {
                _onCallRate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CalculationSummary));
                OnPropertyChanged(nameof(CalculatedPay));
                ValidateInput();
                ValidationChanged?.Invoke();
            }
        }

        public string Notes
        {
            get => _notes;
            set
            {
                _notes = value;
                OnPropertyChanged();
            }
        }

        public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

        public string ValidationMessage
        {
            get => _validationMessage;
            set
            {
                _validationMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasValidationMessage));
            }
        }

        // Visibility
        public bool ShowCalculation => true;
        public bool ShowActiveHoursInfo => GetActiveHoursValue() > 0;

        // Display texts
        public string OnCallExplanationText
        {
            get
            {
                if (_activeJob?.EmploymentType == EmploymentType.Temporary)
                {
                    return "Som timanställd: Jourersättning + timlön för aktiv arbetstid måste avtalas separat.";
                }
                else
                {
                    return "Som fast anställd: Jourersättning enligt avtal + vanlig lön för aktiv arbetstid.";
                }
            }
        }

        public string StandbyHoursText
        {
            get
            {
                var hours = CalculateStandbyHours();
                return $"{hours:F1} timmar jour";
            }
        }

        public string CalculationSummary
        {
            get
            {
                var standbyHours = CalculateStandbyHours();
                var activeHours = GetActiveHoursValue();
                var onCallRate = GetOnCallRateValue();

                if (standbyHours <= 0)
                    return "Kontrollera jourtider";

                var summary = $"Jour: {standbyHours:F1}h × {onCallRate:N0} kr = {(standbyHours * onCallRate):N0} kr";

                if (activeHours > 0)
                {
                    var hourlyRate = GetHourlyRate();
                    var activePay = activeHours * hourlyRate;
                    summary += $"\nAktiv tid: {activeHours:F1}h × {hourlyRate:N0} kr = {activePay:N0} kr";
                }

                return summary;
            }
        }

        public decimal CalculatedPay
        {
            get
            {
                var standbyHours = CalculateStandbyHours();
                var activeHours = GetActiveHoursValue();
                var onCallRate = GetOnCallRateValue();
                var hourlyRate = GetHourlyRate();

                var standbyPay = standbyHours * onCallRate;
                var activePay = activeHours * hourlyRate;

                return standbyPay + activePay;
            }
        }

        #endregion

        #region Public Methods

        public void UpdateContext(DateTime selectedDate, JobProfile activeJob)
        {
            _selectedDate = selectedDate;
            _activeJob = activeJob;

            // Uppdatera alla properties
            OnPropertyChanged(nameof(OnCallExplanationText));
            OnPropertyChanged(nameof(CalculationSummary));
            OnPropertyChanged(nameof(CalculatedPay));
            OnPropertyChanged(nameof(StandbyHoursText));
            ValidateInput();
        }

        public bool CanSave()
        {
            if (_activeJob == null)
                return false;

            if (CalculateStandbyHours() <= 0)
                return false;

            if (GetOnCallRateValue() <= 0)
                return false;

            if (GetActiveHoursValue() < 0)
                return false;

            return string.IsNullOrEmpty(ValidationMessage);
        }

        public async Task<bool> SaveOnCall()
        {
            try
            {
                if (!CanSave())
                {
                    ValidationMessage = "Kontrollera att alla obligatoriska fält är ifyllda";
                    return false;
                }

                // Använd OnCallHandler för att skapa jourpass
                var workShift = _onCallHandler.CreateOnCallShift(
                    _activeJob.Id,
                    _selectedDate,
                    _standbyStartTime,
                    _standbyEndTime,
                    GetActiveHoursValue(),
                    GetOnCallRateValue(),
                    _notes
                );

                return workShift != null;
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Fel vid sparande: {ex.Message}";
                return false;
            }
        }

        #endregion

        #region Private Methods

        private decimal CalculateStandbyHours()
        {
            var start = _standbyStartTime;
            var end = _standbyEndTime;

            // Hantera över midnatt
            if (end <= start)
            {
                end = end.Add(TimeSpan.FromDays(1));
            }

            var duration = end - start;
            return (decimal)duration.TotalHours;
        }

        private decimal GetActiveHoursValue()
        {
            if (string.IsNullOrWhiteSpace(ActiveHours))
                return 0;

            var normalizedInput = ActiveHours.Replace(',', '.');
            if (decimal.TryParse(normalizedInput, System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out decimal result))
            {
                return Math.Max(0, result);
            }
            return 0;
        }

        private decimal GetOnCallRateValue()
        {
            if (string.IsNullOrWhiteSpace(OnCallRate))
                return 40; // Default

            var normalizedInput = OnCallRate.Replace(',', '.');
            if (decimal.TryParse(normalizedInput, System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out decimal result))
            {
                return Math.Max(0, result);
            }
            return 40;
        }

        private decimal GetHourlyRate()
        {
            if (_activeJob?.EmploymentType == EmploymentType.Temporary)
            {
                return _activeJob.HourlyRate ?? 0;
            }
            else if (_activeJob?.MonthlySalary > 0)
            {
                decimal monthlyHours = _activeJob.ExpectedHoursPerMonth > 0
                    ? _activeJob.ExpectedHoursPerMonth
                    : 173.33m;
                return _activeJob.MonthlySalary.Value / monthlyHours;
            }
            return 0;
        }

        private void ValidateInput()
        {
            ValidationMessage = "";

            var standbyHours = CalculateStandbyHours();
            if (standbyHours <= 0)
            {
                ValidationMessage = "Jourtid måste vara längre än 0 timmar";
                return;
            }

            if (standbyHours > 24)
            {
                ValidationMessage = "Jourtid kan inte vara längre än 24 timmar";
                return;
            }

            var activeHours = GetActiveHoursValue();
            if (activeHours < 0)
            {
                ValidationMessage = "Ogiltigt värde för aktiv arbetstid";
                return;
            }

            if (activeHours > standbyHours)
            {
                ValidationMessage = "Aktiv arbetstid kan inte vara längre än jourtid";
                return;
            }

            var onCallRate = GetOnCallRateValue();
            if (onCallRate <= 0)
            {
                ValidationMessage = "Jourersättning måste vara större än 0";
                return;
            }
        }

        #endregion

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
