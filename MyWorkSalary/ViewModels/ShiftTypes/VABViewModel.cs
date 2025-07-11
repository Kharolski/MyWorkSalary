using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Services.Handlers;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyWorkSalary.ViewModels.ShiftTypes
{
    public class VABViewModel : INotifyPropertyChanged
    {
        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly IShiftValidationService _validationService;
        private readonly VABHandler _vabHandler;                    

        private DateTime _selectedDate;
        private JobProfile _activeJob;
        private string _workingHours = "";
        private string _notes = "";
        private string _validationMessage = "";

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action ValidationChanged;

        #region Constructor
        public VABViewModel(
            IWorkShiftRepository workShiftRepository,
            IShiftValidationService validationService,
            VABHandler vabHandler)                               
        {
            _workShiftRepository = workShiftRepository;
            _validationService = validationService;
            _vabHandler = vabHandler;                            
        }
        #endregion

        #region Properties
        public string WorkingHours
        {
            get => _workingHours;
            set
            {
                _workingHours = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CalculationSummary));
                OnPropertyChanged(nameof(PayDeductionText));
                OnPropertyChanged(nameof(ShowPayDeduction));
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
        public bool ShowVABWorkingHours => _activeJob?.IsHourlyEmployee != true;
        public bool ShowCalculation => true;
        public bool ShowPayDeduction => ShowVABWorkingHours && GetWorkingHoursValue() > 0;

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
                    var hours = GetWorkingHoursValue();
                    if (hours > 0)
                    {
                        return $"VAB-dag: {hours:F1} timmar skulle jobbats";
                    }
                    else if (hours == 0)
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

                var hours = GetWorkingHoursValue();

                if (_activeJob?.IsHourlyEmployee != true && _activeJob?.MonthlySalary > 0)
                {
                    // Månadslönad - visa dagavdrag
                    var dailyDeduction = _activeJob.MonthlySalary.Value / 21;
                    var totalDeduction = (hours / 8) * dailyDeduction;
                    return $"Löneavdrag: {totalDeduction:N0} kr (dagavdrag för {hours:F1}t)";
                }
                else
                {
                    // Timanställd - visa förlorad timlön
                    var hourlyRate = GetHourlyRate();
                    var deduction = hours * hourlyRate;
                    return $"Förlorad lön: {deduction:N0} kr ({hours:F1}t × {hourlyRate:N0} kr/t)";
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

            // För fast anställda: kräv timmar
            if (ShowVABWorkingHours)
            {
                if (string.IsNullOrWhiteSpace(WorkingHours))
                    return false;

                var hoursValue = GetWorkingHoursValue();
                if (hoursValue < 0)
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

                // Förbered tider för delvis VAB (om relevant)
                TimeSpan? startTime = null;
                TimeSpan? endTime = null;

                // Om det är delvis VAB (månadslönad som jobbade en del)
                if (!_activeJob.IsHourlyEmployee)
                {
                    var scheduledHours = GetWorkingHoursValue();
                    if (scheduledHours > 0 && scheduledHours < 8) // Delvis dag
                    {
                        // Exempel: Om 4h skulle jobbats, sätt 08:00-12:00
                        startTime = TimeSpan.FromHours(8);
                        endTime = TimeSpan.FromHours(8 + (double)scheduledHours);
                    }
                }

                // Använd VABHandler för att hantera logik
                var result = await _vabHandler.HandleVAB(_selectedDate, _activeJob, startTime, endTime);

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
                            var confirmResult = await _vabHandler.ConfirmReplaceWithVAB(_selectedDate, _activeJob);
                            if (!confirmResult.Success)
                            {
                                ValidationMessage = confirmResult.Message;
                                return false;
                            }
                        }
                        else
                        {
                            return false; // Användaren avbröt
                        }
                    }
                    else
                    {
                        ValidationMessage = result.Message;
                        return false;
                    }
                }

                // Lägg till användarens anteckningar om det behövs
                if (!string.IsNullOrWhiteSpace(_notes) && result.CreatedShift != null)
                {
                    result.CreatedShift.Notes = _notes;
                    _workShiftRepository.SaveWorkShift(result.CreatedShift);
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

        private decimal GetWorkingHoursValue()
        {
            if (string.IsNullOrWhiteSpace(WorkingHours))
                return -1;

            var normalizedInput = WorkingHours.Replace(',', '.');
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
                if (string.IsNullOrWhiteSpace(WorkingHours))
                {
                    ValidationMessage = "* Ange antal timmar som skulle jobbats";
                    return;
                }

                var hours = GetWorkingHoursValue();
                if (hours < 0)
                {
                    ValidationMessage = "Ogiltigt timvärde. Använd siffror (t.ex. 8 eller 7.5)";
                    return;
                }

                if (hours > 24)
                {
                    ValidationMessage = "Arbetstid kan inte vara längre än 24 timmar";
                    return;
                }
            }
        }
        #endregion

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
