using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services;
using MyWorkSalary.Services.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyWorkSalary.ViewModels.ShiftTypes;

public class VABViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _databaseService;
    private readonly IShiftValidationService _validationService;

    private DateTime _selectedDate;
    private JobProfile _activeJob;
    private string _workingHours = "";
    private string _notes = "";
    private string _validationMessage = "";

    public event PropertyChangedEventHandler PropertyChanged;

    // Meddela när validering ändras
    public event Action ValidationChanged;

    #region Constructor
    public VABViewModel(DatabaseService databaseService, IShiftValidationService validationService)
    {
        _databaseService = databaseService;
        _validationService = validationService;
    }
    #endregion

    #region Properties

    public string WorkingHours
    {
        get
        {
            return _workingHours;
        }
        set
        {
            _workingHours = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CalculationSummary));
            OnPropertyChanged(nameof(PayDeductionText));
            OnPropertyChanged(nameof(ShowPayDeduction));

            ValidateInput();

            // Meddela AddShiftViewModel att validering behöver uppdateras
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
            var hourlyRate = GetHourlyRate();
            var deduction = hours * hourlyRate;

            return $"Löneavdrag: {deduction:N0} kr ({hours:F1}t × {hourlyRate:N0} kr/t)";
        }
    }

    #endregion

    #region Public Methods

    // Enklare UpdateContext
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
        // Grundläggande validering
        if (_activeJob == null)
        {
            return false;
        }

        // För fast anställda: kräv timmar
        if (ShowVABWorkingHours)
        {
            if (string.IsNullOrWhiteSpace(WorkingHours))
            {
                return false;
            }

            var hoursValue = GetWorkingHoursValue();

            if (hoursValue < 0)
            {
                return false;
            }
        }

        // Inga valideringsfel
        var hasValidationErrors = !string.IsNullOrEmpty(ValidationMessage);

        var result = !hasValidationErrors;
        return result;
    }

    // SaveVAB för en dag
    public async Task<bool> SaveVAB()
    {
        try
        {
            if (!CanSave())
            {
                ValidationMessage = "Kontrollera att alla obligatoriska fält är ifyllda";
                return false;
            }

            // Kontrollera om det redan finns ett pass
            if (_validationService.HasShiftOnDate(_activeJob.Id, _selectedDate))
            {
                // Använd annat variabelnamn
                var currentShift = _databaseService.WorkShifts.GetWorkShifts(_activeJob.Id)
                    .FirstOrDefault(s => s.ShiftDate.Date == _selectedDate.Date);

                string existingShiftInfo = "";
                if (currentShift != null)
                {
                    existingShiftInfo = currentShift.ShiftType switch
                    {
                        ShiftType.Regular => $"Arbetspass ({currentShift.StartTime:HH:mm} - {currentShift.EndTime:HH:mm})",
                        ShiftType.SickLeave => $"Sjukskrivning ({currentShift.NumberOfDays} dagar)",
                        ShiftType.Vacation => $"Semester ({currentShift.NumberOfDays} dagar)",
                        ShiftType.VAB => "VAB-dag",
                        ShiftType.OnCall => "Jour",
                        _ => "Okänt pass"
                    };
                }

                // Visa bekräftelsedialog
                bool userConfirmed = await Shell.Current.DisplayAlert(
                    "Ersätt befintligt pass?",
                    $"Det finns redan ett pass registrerat för {_selectedDate:yyyy-MM-dd}:\n\n" +
                    $"📅 Befintligt: {existingShiftInfo}\n" +
                    $"🆕 Nytt: VAB-dag\n\n" +
                    $"Vill du ersätta det befintliga passet med VAB?",
                    "Ja, ersätt",
                    "Avbryt"
                );

                if (!userConfirmed)
                {
                    return false; // Användaren avbröt
                }
            }

            // Hämta befintligt pass för sparande
            var existingShift = _databaseService.WorkShifts.GetWorkShifts(_activeJob.Id)
                .FirstOrDefault(s => s.ShiftDate.Date == _selectedDate.Date);

            WorkShift shift;
            if (existingShift != null)
            {
                // Uppdatera befintligt pass
                shift = existingShift;
                shift.ModifiedDate = DateTime.Now;
            }
            else
            {
                // Skapa nytt pass
                shift = new WorkShift
                {
                    JobProfileId = _activeJob.Id,
                    ShiftDate = _selectedDate,
                    CreatedDate = DateTime.Now
                };
            }

            // Sätt VAB-specifika värden
            shift.ShiftType = ShiftType.VAB;
            shift.NumberOfDays = 1;
            shift.Notes = _notes; 

            // Hantera arbetstimmar
            var hours = GetWorkingHoursValue();
            if (_activeJob?.IsHourlyEmployee == true)
            {
                // Timanställd: inga arbetstider eller lön
                shift.StartTime = null;
                shift.EndTime = null;
                shift.TotalHours = 0;
                shift.RegularPay = 0;
            }
            else
            {
                // Fast anställd: spara timmar som NEGATIVT värde
                shift.StartTime = null;
                shift.EndTime = null;
                // VAB = NEGATIVT värde (användaren förlorar arbetstimmar)
                shift.TotalHours = -(decimal)hours;  // ← NEGATIVT!
                                                     // Beräkna löneavdrag (negativt värde)
                if (hours > 0)
                {
                    shift.RegularPay = (decimal)hours * GetHourlyRate() * -1;
                }
                else
                {
                    shift.RegularPay = 0; // Ingen lön att dra av om man skulle vara ledig
                }
            }

            shift.TotalPay = shift.RegularPay; // VAB ger ingen extra lön från företaget

            // Spara shift
            _databaseService.WorkShifts.SaveWorkShift(shift);
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
    // Helper metod för att få timlön
    private decimal GetHourlyRate()
    {
        if (_activeJob?.IsHourlyEmployee == true)
        {
            // Timanställd: använd HourlyRate direkt
            return _activeJob.HourlyRate ?? 0;
        }
        else if (_activeJob?.MonthlySalary > 0)
        {
            decimal monthlyHours = _activeJob.ExpectedHoursPerMonth > 0
                ? _activeJob.ExpectedHoursPerMonth
                : 173.33m; // Fallback

            return _activeJob.MonthlySalary.Value / monthlyHours;
        }

        return 0;
    }

    // Konvertera WorkingHours string till decimal
    private decimal GetWorkingHoursValue()
    {
        if (string.IsNullOrWhiteSpace(WorkingHours))
        {
            return -1; // Ogiltigt
        }

        // Försök parsa med både punkt och komma som decimaltecken
        var normalizedInput = WorkingHours.Replace(',', '.');

        if (decimal.TryParse(normalizedInput, System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture, out decimal result))
        {
            return result;
        }

        return -1; // Ogiltigt värde
    }

    private void ValidateInput()
    {
        ValidationMessage = "";

        // För fast anställda: validera arbetstimmar
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
