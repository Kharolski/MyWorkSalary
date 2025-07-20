using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
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
                    // Använd ValidateVacationDate metod
                    var (canAdd, errorMessage, conflictingShifts) = _validationService.ValidateVacationDate(
                           workShift.JobProfileId,
                           workShift.ShiftDate,
                           VacationType.PaidVacation); // Standard för nu

                    if (!canAdd)
                        return (false, errorMessage);
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
                _databaseService.WorkShifts.SaveWorkShift(workShift);
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

        // För converter-logik:
        public async Task<string> GetSickLeaveHoursDisplayAsync(int workShiftId)
        {
            try
            {
                // Hämta SickLeave från databas via WorkShiftId
                var sickLeave = _databaseService.SickLeaves.GetSickLeaveByWorkShiftId(workShiftId);
                if (sickLeave == null)
                {
                    return "0t";
                }

                return sickLeave.SickType switch
                {
                    SickLeaveType.ShouldHaveWorked => "-8t",  // Hel dag sjuk
                    SickLeaveType.WorkedPartially => $"-{(sickLeave.ScheduledHours - sickLeave.WorkedHours):F1}t",  // Delvis sjuk
                    SickLeaveType.WouldBeFree => "0t",  // Skulle varit ledig ändå
                    _ => "0t"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fel i GetSickLeaveHoursDisplayAsync: {ex.Message}");
                return "0t";
            }
        }

        public async Task<string> GetSickLeaveDescriptionAsync(int workShiftId)
        {
            try
            {
                var sickLeave = _databaseService.SickLeaves.GetSickLeaveByWorkShiftId(workShiftId);
                if (sickLeave == null)
                {
                    return "Sjukskrivning";
                }

                return sickLeave.SickType switch
                {
                    SickLeaveType.WorkedPartially => $"Sjukskrivning - delvis ({sickLeave.WorkedHours:F1}t)",
                    SickLeaveType.ShouldHaveWorked => "Sjukskrivning - hel dag",
                    SickLeaveType.WouldBeFree => "Sjukskrivning - ledigt",
                    _ => "Sjukskrivning"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fel i GetSickLeaveDescriptionAsync: {ex.Message}");
                return "Sjukskrivning";
            }
        }
    }
}
