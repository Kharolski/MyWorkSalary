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
                    return "Som timanställd: Ingen arbetstid = ingen lön från företaget. Försäkringskassan betalar VAB-ersättning.";
                }
                else
                {
                    return "Som fast anställd: Företaget drar arbetstid från lön. Försäkringskassan ersätter med VAB-pengar.";
                }
            }
        }

        public string CalculationSummary
        {
            get
            {
                if (_activeJob?.IsHourlyEmployee == true)
                {
                    return "VAB-dag registrerad - Ingen lön från företaget";
                }
                else
                {
                    var scheduledHours = GetScheduledHoursValue();
                    var workedHours = GetWorkedHoursValue();

                    if (scheduledHours > 0 && workedHours >= 0)
                    {
                        var nettoHours = workedHours - scheduledHours;
                        return $"VAB: Jobbade {workedHours:F1}t, skulle {scheduledHours:F1}t = {nettoHours:F1}t netto";
                    }
                    else if (scheduledHours == 0)
                    {
                        return "VAB-dag: Skulle varit ledig (0 timmar)";
                    }
                    else
                    {
                        return "VAB-dag: Ange timmar som skulle jobbats";
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

                if (_activeJob?.IsHourlyEmployee != true && _activeJob?.MonthlySalary > 0)
                {
                    // Månadslönad - visa både lön och avdrag
                    var dailyDeduction = _activeJob.MonthlySalary.Value / 21;
                    var hourlyRate = GetHourlyRate();

                    var workedPay = workedHours * hourlyRate;
                    var scheduledDeduction = (scheduledHours / 8) * dailyDeduction;
                    var nettoEffect = workedPay - scheduledDeduction;

                    return $"Lön: +{workedPay:N0} kr, Avdrag: -{scheduledDeduction:N0} kr = {nettoEffect:N0} kr netto";
                }
                else
                {
                    // Timanställd - bara förlorad lön
                    var hourlyRate = GetHourlyRate();
                    var workedPay = workedHours * hourlyRate;
                    var lostPay = (scheduledHours - workedHours) * hourlyRate;

                    return $"Lön: {workedPay:N0} kr, Förlorat: -{lostPay:N0} kr";
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
                    ValidationMessage = "Kontrollera att alla obligatoriska fält är ifyllda";
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
                            "Ersätt befintligt pass?",
                            result.ConfirmationMessage,
                            "Ja, ersätt",
                            "Avbryt"
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
                        ValidationMessage = $"Konstig meddelande efter avbryt:  result.Message";
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Fel vid sparande: {ex.Message}";
                return false;
            }
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
                    ValidationMessage = "* Ange antal timmar som skulle jobbats";
                    return;
                }

                var scheduledHours = GetScheduledHoursValue();
                if (scheduledHours < 0)
                {
                    ValidationMessage = "Ogiltigt värde för 'skulle jobbat'. Använd siffror (t.ex. 8 eller 7.5)";
                    return;
                }

                if (scheduledHours > 24)
                {
                    ValidationMessage = "Arbetstid kan inte vara längre än 24 timmar";
                    return;
                }

                // Validera WorkedHours (obligatoriskt)
                if (string.IsNullOrWhiteSpace(WorkedHours))
                {
                    ValidationMessage = "* Ange antal timmar som faktiskt jobbades";
                    return;
                }

                var workedHours = GetWorkedHoursValue();
                if (workedHours < 0)
                {
                    ValidationMessage = "Ogiltigt värde för 'jobbade timmar'. Använd siffror (t.ex. 4 eller 0)";
                    return;
                }

                if (workedHours > 24)
                {
                    ValidationMessage = "Jobbade timmar kan inte vara längre än 24 timmar";
                    return;
                }

                if (workedHours > scheduledHours)
                {
                    ValidationMessage = "Jobbade timmar kan inte vara fler än planerade timmar";
                    return;
                }

                if (workedHours == scheduledHours && scheduledHours > 0)
                {
                    ValidationMessage = "💡 Ingen VAB behövs - du jobbade alla planerade timmar. Registrera som vanligt arbetspass istället.";
                    return;
                }
            }
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
