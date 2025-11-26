using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Services.Handlers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MyWorkSalary.Helpers.Localization;

namespace MyWorkSalary.ViewModels.ShiftTypes
{
    public class OnCallViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly IShiftValidationService _validationService;
        private readonly OnCallHandler _onCallHandler;
        private bool _disposed = false;

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

            LocalizationHelper.LanguageChanged += OnLanguageChanged;
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

                NotifyLocalizedProperties();
                OnPropertyChanged(nameof(CalculatedPay));
                OnPropertyChanged(nameof(FormattedCalculatedPay));
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

                NotifyLocalizedProperties();
                OnPropertyChanged(nameof(CalculatedPay));
                OnPropertyChanged(nameof(FormattedCalculatedPay));
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

                NotifyLocalizedProperties();
                OnPropertyChanged(nameof(CalculatedPay));
                OnPropertyChanged(nameof(FormattedCalculatedPay));
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

                NotifyLocalizedProperties();
                OnPropertyChanged(nameof(CalculatedPay));
                OnPropertyChanged(nameof(FormattedCalculatedPay));
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
                    return LocalizationHelper.Translate("OnCall_Explanation_Temporary");
                }
                else
                {
                    return LocalizationHelper.Translate("OnCall_Explanation_Permanent");
                }
            }
        }

        public string StandbyHoursText
        {
            get
            {
                var hours = CalculateStandbyHours();
                return string.Format(
                    LocalizationHelper.Translate("OnCall_StandbyHours"),
                    hours,
                    LocalizationHelper.Translate("HoursAbbreviation")
                );
            }
        }

        public string CalculationSummary
        {
            get
            {
                var standbyHours = CalculateStandbyHours();
                var activeHours = GetActiveHoursValue();
                var onCallRate = GetOnCallRateValue();
                var hourlyRate = GetHourlyRate();
                var currencyCode = _activeJob?.CurrencyCode ?? "SEK";

                // Om tiderna är fel
                if (standbyHours <= 0)
                    return LocalizationHelper.Translate("OnCall_CheckTimes");

                // Lokala etiketter
                var standbyLabel = LocalizationHelper.Translate("OnCall_StandbyLabel");              // ex. "Jour"
                var activeLabel = LocalizationHelper.Translate("OnCall_ActiveLabel");   // ex. "Aktiv tid"
                var hoursAbbr = LocalizationHelper.Translate("HoursAbbreviation");      // ex. "h"

                // Jourdel
                var standbyPay = standbyHours * onCallRate;
                var formattedStandbyPay = CurrencyHelper.FormatCurrency(standbyPay, currencyCode);
                var summary = $"{standbyLabel}: {standbyHours:F1}{hoursAbbr} × {onCallRate:N0} = {formattedStandbyPay}";

                // Aktiv del (endast om aktiv tid finns)
                if (activeHours > 0)
                {
                    var activePay = activeHours * hourlyRate;
                    var formattedActivePay = CurrencyHelper.FormatCurrency(activePay, currencyCode);
                    summary += $"\n{activeLabel}: {activeHours:F1}{hoursAbbr} × {hourlyRate:N0} = {formattedActivePay}";
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

        public string FormattedCalculatedPay
        {
            get
            {
                var currencyCode = _activeJob?.CurrencyCode ?? "SEK";
                return CurrencyHelper.FormatCurrency(CalculatedPay, currencyCode);
            }
        }

        public string TotalEstimateText
        {
            get
            {
                // Hämta översatt text, t.ex. "Preliminärt totalt: {0}"
                var label = LocalizationHelper.Translate("OnCall_TotalEstimate");

                var currencyCode = _activeJob?.CurrencyCode ?? "SEK";
                var formatted = CurrencyHelper.FormatCurrency(CalculatedPay, currencyCode);

                // Sätt in värdet
                return string.Format(label, formatted);
            }
        }

        public string RateTipText =>
            string.Format(
                LocalizationHelper.Translate("OnCall_RateTip"),
                _activeJob != null ? CurrencyHelper.GetSymbol(_activeJob.CurrencyCode) : "kr"
            );

        #endregion

        #region Public Methods

        public void UpdateContext(DateTime selectedDate, JobProfile activeJob)
        {
            _selectedDate = selectedDate;
            _activeJob = activeJob;

            // Uppdatera språk- och valuta-beroende properties
            NotifyLocalizedProperties();

            // Uppdatera övriga properties
            OnPropertyChanged(nameof(CalculatedPay));
            OnPropertyChanged(nameof(FormattedCalculatedPay));
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
                    ValidationMessage = LocalizationHelper.Translate("OnCall_Save_ErrorMissing");
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
                ValidationMessage = string.Format(
                    LocalizationHelper.Translate("OnCall_Save_Exception"),
                    ex.Message
                );
                return false;
            }
        }

        public void Reset()
        {
            // Återställ core-data
            _standbyStartTime = new TimeSpan(18, 0, 0);  // 18:00 standard
            _standbyEndTime = new TimeSpan(8, 0, 0);     // 08:00 standard
            _activeHours = "0";
            _onCallRate = "40";
            _notes = "";
            _validationMessage = "";

            // Trigger UI updates
            OnPropertyChanged(nameof(StandbyStartTime));
            OnPropertyChanged(nameof(StandbyEndTime));
            OnPropertyChanged(nameof(ActiveHours));
            OnPropertyChanged(nameof(OnCallRate));
            OnPropertyChanged(nameof(Notes));
            OnPropertyChanged(nameof(ValidationMessage));
            OnPropertyChanged(nameof(HasValidationMessage));

            // Lokala språkberoende properties
            NotifyLocalizedProperties();

            // Om beroenden finns
            OnPropertyChanged(nameof(CalculatedPay));
            OnPropertyChanged(nameof(FormattedCalculatedPay));
            OnPropertyChanged(nameof(ShowActiveHoursInfo));

            // Kör validering
            ValidateInput();
            ValidationChanged?.Invoke();
        }
        #endregion

        #region Private Methods
        private void OnLanguageChanged()
        {
            // Försöksskydd mot att metoden körs efter dispose
            if (_disposed)
                return;

            // Update all localized properties
            NotifyLocalizedProperties();

            // Update other properties that might depend on language
            OnPropertyChanged(nameof(CalculatedPay));
            OnPropertyChanged(nameof(FormattedCalculatedPay));
            OnPropertyChanged(nameof(ShowActiveHoursInfo));
        }

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
                ValidationMessage = LocalizationHelper.Translate("OnCall_Validation_ZeroHours");
                return;
            }

            if (standbyHours > 24)
            {
                ValidationMessage = LocalizationHelper.Translate("OnCall_Validation_TooLong");
                return;
            }

            var activeHours = GetActiveHoursValue();
            if (activeHours < 0)
            {
                ValidationMessage = LocalizationHelper.Translate("OnCall_Validation_InvalidActive");
                return;
            }

            if (activeHours > standbyHours)
            {
                ValidationMessage = LocalizationHelper.Translate("OnCall_Validation_ActiveTooLong");
                return;
            }

            var onCallRate = GetOnCallRateValue();
            if (onCallRate <= 0)
            {
                ValidationMessage = LocalizationHelper.Translate("OnCall_Validation_RateTooLow");
                return;
            }
        }

        /// <summary>
        /// Triggar OnPropertyChanged för alla properties som beror på språk/valuta.
        /// Används när t.ex. språk eller valuta ändras.
        /// </summary>
        private void NotifyLocalizedProperties()
        {
            OnPropertyChanged(nameof(OnCallExplanationText));
            OnPropertyChanged(nameof(StandbyHoursText));
            OnPropertyChanged(nameof(CalculationSummary));
            OnPropertyChanged(nameof(TotalEstimateText));
            OnPropertyChanged(nameof(RateTipText));
        }
        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            if (!_disposed)
            {
                LocalizationHelper.LanguageChanged -= OnLanguageChanged;
                _disposed = true;
            }
        }

        #endregion

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
