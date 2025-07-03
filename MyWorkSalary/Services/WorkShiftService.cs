using MyWorkSalary.Models;
using MyWorkSalary.Services.Interfaces;
using System.Globalization;

namespace MyWorkSalary.Services
{
    public class WorkShiftService : IWorkShiftService
    {
        private readonly DatabaseService _databaseService;
        private readonly IShiftValidationService _validationService;
        private readonly IConflictResolutionService _conflictResolutionService;

        public WorkShiftService(
            DatabaseService databaseService,
            IShiftValidationService validationService,
            IConflictResolutionService conflictResolutionService)
        {
            _databaseService = databaseService;
            _validationService = validationService;
            _conflictResolutionService = conflictResolutionService;
        }

        public async Task<(bool Success, string Message)> SaveWorkShiftWithValidation(WorkShift workShift)
        {
            try
            {
                // Specialhantering för sjukskrivning
                if (workShift.ShiftType == ShiftType.SickLeave)
                {
                    return await _conflictResolutionService.SaveSickLeaveWithConflictResolution(workShift);
                }

                // Kontrollera arbetspass mot hela dagen ledighet
                if (workShift.ShiftType != ShiftType.Vacation && workShift.ShiftType != ShiftType.SickLeave)
                {
                    var fullDayConflict = _validationService.CheckWorkShiftAgainstFullDayLeave(workShift);
                    if (fullDayConflict.HasConflict)
                    {
                        var leaveType = fullDayConflict.ConflictingLeave.ShiftType == ShiftType.SickLeave ? "SICK" : "VACATION";
                        return (false, $"{leaveType}_CONFLICT|{fullDayConflict.ConflictMessage}|{fullDayConflict.ConflictingLeave.Id}|{workShift.StartTime.Value.Date:yyyy-MM-dd}");
                    }
                }

                // Kontrollera arbetspass mot sjukskrivning
                if (workShift.ShiftType != ShiftType.Vacation && workShift.ShiftType != ShiftType.SickLeave)
                {
                    var sickConflict = _conflictResolutionService.CheckWorkShiftAgainstSickLeave(workShift);
                    if (sickConflict.HasConflict)
                    {
                        return (false, $"SICK_CONFLICT|{sickConflict.ConflictMessage}|{sickConflict.ConflictingSickLeave.Id}|{workShift.StartTime.Value.Date:yyyy-MM-dd}");
                    }
                }

                // Validera semester
                if (workShift.ShiftType == ShiftType.Vacation)
                {
                    var vacationValidation = _validationService.ValidateVacation(workShift);
                    if (!vacationValidation.IsValid)
                        return (false, vacationValidation.Message);
                }

                // Kontrollera ledighetskonflikt
                var conflictCheck = _conflictResolutionService.GetLeaveConflictDetails(workShift);
                if (conflictCheck.HasConflict)
                {
                    return (false, conflictCheck.ConflictMessage);
                }

                // Kontrollera tidsöverlapp för vanliga pass
                var overlappingShift = _validationService.GetOverlappingShift(workShift);
                if (overlappingShift != null)
                {
                    var swedishCulture = new System.Globalization.CultureInfo("sv-SE");
                    var message = $"Passet överlappar med befintligt pass:\n\n" +
                                 $"📅 {overlappingShift.StartTime?.ToString("dddd d MMMM", swedishCulture) ?? overlappingShift.ShiftDate.ToString("dddd d MMMM", swedishCulture)}\n" +
                                 $"🕐 {overlappingShift.StartTime?.ToString("HH:mm")} → {overlappingShift.EndTime?.ToString("HH:mm")}\n\n" +
                                 $"Ändra tiden för att undvika överlapp.";
                    return (false, message);
                }

                // Spara om allt är ok
                _databaseService.SaveWorkShift(workShift);
                string successMessage = workShift.ShiftType switch
                {
                    ShiftType.Vacation => $"Semester på {workShift.NumberOfDays} dagar har sparats!",
                    _ => "Passet har sparats!"
                };
                return (true, successMessage);
            }
            catch (Exception ex)
            {
                return (false, $"Fel vid sparande: {ex.Message}");
            }
        }
    }
}
