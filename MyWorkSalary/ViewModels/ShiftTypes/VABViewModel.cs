using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Services.Handlers;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyWorkSalary.ViewModels.ShiftTypes
{
    public class VABViewModel : INotifyPropertyChanged
    {
        #region Field
        private readonly VABHandler _vabHandler;

        private DateTime _selectedDate;
        private JobProfile _activeJob;
        private string _scheduledHours = "";
        private string _workedHours = "";
        private string _validationMessage = "";

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action ValidationChanged;
        #endregion

        #region Constructor
        public VABViewModel(VABHandler vabHandler)                               
        {
            _vabHandler = vabHandler;

            LocalizationHelper.LanguageChanged += OnLanguageChanged;
        }
        #endregion

        #region Properties
        public string ScheduledHours
        {
            get => _scheduledHours;
            set
            {
                _scheduledHours = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CalculationSummary));
                OnPropertyChanged(nameof(PayDeductionText));
                OnPropertyChanged(nameof(ShowPayDeduction));
                ValidateInput();
                ValidationChanged?.Invoke();
            }
        }

        public string WorkedHours
        {
            get => _workedHours;
            set
            {
                _workedHours = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CalculationSummary));
                OnPropertyChanged(nameof(PayDeductionText));
                OnPropertyChanged(nameof(ShowPayDeduction));
                ValidateInput();
                ValidationChanged?.Invoke();
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
        public bool ShowVABWorkingHours => _activeJob?.IsHourlyEmployee != true;
        public bool ShowCalculation => true;
        public bool ShowPayDeduction => ShowVABWorkingHours && GetScheduledHoursValue() > 0;

        // Beräkningar
        public string VABExplanationText
        {
            get
            {
                if (_activeJob?.IsHourlyEmployee == true)
                {
                    return LocalizationHelper.Translate("VAB_Explanation_Hourly");
                }
                else
                {
                    return LocalizationHelper.Translate("VAB_Explanation_Salaried");
                }
            }
        }

        public string CalculationSummary
        {
            get
            {
                // Timanställd: enkel text från resx
                if (_activeJob?.IsHourlyEmployee == true)
                {
                    return LocalizationHelper.Translate("VAB_Summary_HourlyEmployee");
                }
                else
                {
                    // Månadsanställd / övrigt
                    var scheduledHours = GetScheduledHoursValue();
                    var workedHours = GetWorkedHoursValue();

                    // Hämta hours-abbr från resx (t.ex. "h" eller "t")
                    var hoursAbbr = LocalizationHelper.Translate("HoursAbbreviation");

                    if (scheduledHours > 0 && workedHours >= 0)
                    {
                        var nettoHours = workedHours - scheduledHours;

                        // resx-strängen har placeholders: {0} = workedHours, {1} = scheduledHours, {2} = nettoHours, {3} = hoursAbbr
                        var template = LocalizationHelper.Translate("VAB_Summary_WorkedVsPlanned");
                        return string.Format(template, workedHours, scheduledHours, nettoHours, hoursAbbr);
                    }
                    else if (scheduledHours == 0)
                    {
                        return LocalizationHelper.Translate("VAB_Summary_ZeroHours");
                    }
                    else
                    {
                        return LocalizationHelper.Translate("VAB_Summary_MissingHours");
                    }
                }
            }
        }

        public string PayDeductionText
        {
            get
            {
                if (!ShowPayDeduction)
                    return "";

                var scheduledHours = GetScheduledHoursValue();
                var workedHours = GetWorkedHoursValue();

                if (scheduledHours <= 0 || workedHours < 0)
                    return "";

                // Hämta symbol för företagets valuta
                var currencySymbol = CurrencyHelper.GetSymbol(_activeJob?.CurrencyCode ?? "SEK");

                if (_activeJob?.IsHourlyEmployee != true && _activeJob?.MonthlySalary > 0)
                {
                    // Månadslönad - visa både lön och avdrag
                    var dailyDeduction = _activeJob.MonthlySalary.Value / 21;
                    var hourlyRate = GetHourlyRate();

                    var workedPay = workedHours * hourlyRate;
                    var scheduledDeduction = (scheduledHours / 8) * dailyDeduction;
                    var nettoEffect = workedPay - scheduledDeduction;

                    // VAB_Pay_Monthly
                    var template = LocalizationHelper.Translate("VAB_Pay_Monthly");

                    return string.Format(template, workedPay, scheduledDeduction, nettoEffect, currencySymbol);
                }
                else
                {
                    // Timanställd - bara förlorad lön
                    var hourlyRate = GetHourlyRate();
                    var workedPay = workedHours * hourlyRate;
                    var lostPay = (scheduledHours - workedHours) * hourlyRate;

                    // VAB_Pay_Hourly
                    var template = LocalizationHelper.Translate("VAB_Pay_Hourly");

                    return string.Format(template, workedPay, lostPay, currencySymbol);
                }
            }
        }
        #endregion

        #region Public Methods
        public void UpdateContext(DateTime selectedDate, JobProfile activeJob)
        {
            _selectedDate = selectedDate;
            _activeJob = activeJob;

            // Uppdatera alla properties
            OnPropertyChanged(nameof(VABExplanationText));
            OnPropertyChanged(nameof(ShowVABWorkingHours));
            OnPropertyChanged(nameof(ShowPayDeduction));
            OnPropertyChanged(nameof(CalculationSummary));
            OnPropertyChanged(nameof(PayDeductionText));
            ValidateInput();
        }

        public bool CanSave()
        {
            if (_activeJob == null)
                return false;

            // För fast anställda: kräv båda timmar-fält
            if (ShowVABWorkingHours)
            {
                if (string.IsNullOrWhiteSpace(ScheduledHours))
                    return false;
                if (string.IsNullOrWhiteSpace(WorkedHours))
                    return false;

                var scheduledValue = GetScheduledHoursValue();
                var workedValue = GetWorkedHoursValue();

                if (scheduledValue < 0 || workedValue < 0)
                    return false;
            }

            return string.IsNullOrEmpty(ValidationMessage);
        }

        /// <summary>
        /// Sparar VAB med nya VABHandler
        /// </summary>
        public async Task<bool> SaveVAB()
        {
            try
            {
                if (!CanSave())
                {
                    ValidationMessage = LocalizationHelper.Translate("VAB_Validation_MissingFields");
                    return false;
                }

                // Hämta båda värdena
                decimal scheduledHours = GetScheduledHoursValue();
                decimal workedHours = GetWorkedHoursValue();

                // Förbered tider för delvis VAB (om relevant)
                TimeSpan? startTime = null;
                TimeSpan? endTime = null;

                // Om det är delvis VAB (jobbade en del av dagen)
                if (workedHours > 0 && workedHours < scheduledHours)
                {
                    // Exempel: Om 4h jobbades, sätt 08:00-12:00
                    startTime = TimeSpan.FromHours(8);
                    endTime = TimeSpan.FromHours(8 + (double)workedHours);
                }

                // Använd VABHandler för att hantera logik
                var result = await _vabHandler.HandleVAB(_selectedDate, _activeJob, startTime, endTime, scheduledHours, workedHours);

                if (!result.Success)
                {
                    if (result.ShowConfirmationDialog)
                    {
                        // Visa bekräftelsedialog
                        bool userConfirmed = await Shell.Current.DisplayAlert(
                            LocalizationHelper.Translate("VAB_Confirm_Replace_Title"),
                            result.ConfirmationMessage,
                            LocalizationHelper.Translate("Dialog_YesReplace"),
                            LocalizationHelper.Translate("Cancel")
                        );

                        if (userConfirmed)
                        {
                            // Bekräfta ersättning
                            var confirmResult = await _vabHandler.ConfirmReplaceWithVAB(_selectedDate, _activeJob, scheduledHours, workedHours);
                            if (!confirmResult.Success)
                            {
                                ValidationMessage = confirmResult.Message;
                                return false;
                            }
                        }
                        else
                        {
                            // Användaren avbröt - rensa eventuellt felmeddelande
                            ValidationMessage = "";
                            return false;
                        }
                    }
                    else
                    {
                        ValidationMessage = string.Format(
                            LocalizationHelper.Translate("VAB_Unexpected_Message"),
                            result.Message
                        );
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                ValidationMessage = string.Format(
                    LocalizationHelper.Translate("VAB_Save_Error"),
                    ex.Message
                );
                return false;
            }
        }

        public void Reset()
        {
            _selectedDate = DateTime.Today;
            _activeJob = null;

            ScheduledHours = "";
            WorkedHours = "";
            ValidationMessage = "";

            // Uppdatera UI
            OnPropertyChanged(nameof(VABExplanationText));
            OnPropertyChanged(nameof(ShowVABWorkingHours));
            OnPropertyChanged(nameof(ShowPayDeduction));
            OnPropertyChanged(nameof(CalculationSummary));
            OnPropertyChanged(nameof(PayDeductionText));

            ValidationChanged?.Invoke();
        }
        #endregion

        #region Private Methods
        private decimal GetHourlyRate()
        {
            if (_activeJob?.IsHourlyEmployee == true)
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

        private decimal GetScheduledHoursValue()
        {
            if (string.IsNullOrWhiteSpace(ScheduledHours))
                return -1;
            var normalizedInput = ScheduledHours.Replace(',', '.');
            if (decimal.TryParse(normalizedInput, System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }
            return -1;
        }

        private decimal GetWorkedHoursValue()
        {
            if (string.IsNullOrWhiteSpace(WorkedHours))
                return -1;
            var normalizedInput = WorkedHours.Replace(',', '.');
            if (decimal.TryParse(normalizedInput, System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }
            return -1;
        }

        private void ValidateInput()
        {
            ValidationMessage = "";

            if (ShowVABWorkingHours)
            {
                // Validera ScheduledHours (obligatoriskt)
                if (string.IsNullOrWhiteSpace(ScheduledHours))
                {
                    ValidationMessage = LocalizationHelper.Translate("VAB_Validation_MissingScheduledHours");
                    return;
                }

                var scheduledHours = GetScheduledHoursValue();
                if (scheduledHours < 0)
                {
                    ValidationMessage = LocalizationHelper.Translate("VAB_Validation_InvalidScheduledHours");
                    return;
                }

                if (scheduledHours > 24)
                {
                    ValidationMessage = LocalizationHelper.Translate("VAB_Validation_ScheduledTooHigh");
                    return;
                }

                // Validera WorkedHours (obligatoriskt)
                if (string.IsNullOrWhiteSpace(WorkedHours))
                {
                    ValidationMessage = LocalizationHelper.Translate("VAB_Validation_MissingWorkedHours");
                    return;
                }

                var workedHours = GetWorkedHoursValue();
                if (workedHours < 0)
                {
                    ValidationMessage = LocalizationHelper.Translate("VAB_Validation_InvalidWorkedHours");
                    return;
                }

                if (workedHours > 24)
                {
                    ValidationMessage = LocalizationHelper.Translate("VAB_Validation_WorkedTooHigh");
                    return;
                }

                if (workedHours > scheduledHours)
                {
                    ValidationMessage = LocalizationHelper.Translate("VAB_Validation_WorkedMoreThanPlanned");
                    return;
                }

                if (workedHours == scheduledHours && scheduledHours > 0)
                {
                    ValidationMessage = LocalizationHelper.Translate("VAB_Validation_NoVABNeeded");
                    return;
                }
            }
        }

        private void OnLanguageChanged()
        {
            OnPropertyChanged(nameof(VABExplanationText));
            OnPropertyChanged(nameof(CalculationSummary));
            OnPropertyChanged(nameof(PayDeductionText));

            if (!string.IsNullOrEmpty(_validationMessage))
                ValidateInput(); // kör om valideringen för att översätta igen om formen är uppe

            OnPropertyChanged(nameof(ValidationMessage));
        }
        #endregion

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Debug Helper
        /// <summary>
        /// Debug-logging som fungerar på både emulator och riktig enhet
        /// Aktivera/inaktivera genom att ändra DEBUG_VAB konstanten
        /// </summary>
        private const bool DEBUG_VAB = false; // Sätt till true för debugging

        private void LogDebug(string message)
        {
            if (!DEBUG_VAB)
                return;
#if ANDROID
            Android.Util.Log.Debug("VABViewModel", message);
#else
    System.Diagnostics.Debug.WriteLine($"VABViewModel: {message}");
#endif
        }
        #endregion
    }
}
