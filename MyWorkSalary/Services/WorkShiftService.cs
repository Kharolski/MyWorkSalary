using Microsoft.Maui.Platform;
using MyWorkSalary.Helpers.Localization;
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
                    var message =
                        LocalizationHelper.Translate("WorkShift_Overlap_Title") + "\n\n" +
                        LocalizationHelper.Translate(
                            "WorkShift_Overlap_Date",
                            (overlappingShift.StartTime ?? overlappingShift.ShiftDate)
                                .ToString("dddd d MMMM", swedishCulture)) + "\n" +
                        LocalizationHelper.Translate(
                            "WorkShift_Overlap_Time",
                            overlappingShift.StartTime?.ToString("HH:mm"),
                            overlappingShift.EndTime?.ToString("HH:mm")) + "\n\n" +
                        LocalizationHelper.Translate("WorkShift_Overlap_Action");
                    return (false, message);
                }

                // Spara om allt är ok
                _databaseService.WorkShifts.SaveWorkShift(workShift);
                string successMessage = workShift.ShiftType switch
                {
                    ShiftType.Vacation => LocalizationHelper.Translate(
                        "WorkShift_Save_Vacation",
                        workShift.NumberOfDays ?? 1),

                    _ => LocalizationHelper.Translate("WorkShift_Save_Generic")
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
                    return LocalizationHelper.Translate("SickLeave_Hours_Zero");
                }

                return sickLeave.SickType switch
                {
                    SickLeaveType.WorkedPartially =>
                        string.Format(
                            LocalizationHelper.Translate("SickLeave_Hours_Worked"),
                            sickLeave.WorkedHours),

                    SickLeaveType.ShouldHaveWorked =>
                        LocalizationHelper.Translate("SickLeave_Hours_Zero"),

                    SickLeaveType.WouldBeFree =>
                        LocalizationHelper.Translate("SickLeave_Hours_Zero"),

                    _ => LocalizationHelper.Translate("SickLeave_Hours_Zero")
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
                    return LocalizationHelper.Translate("SickLeave_Default");
                }

                return sickLeave.SickType switch
                {
                    SickLeaveType.WorkedPartially =>
                        string.Format(
                            LocalizationHelper.Translate("SickLeave_Description_Partial"),
                            sickLeave.WorkedHours),

                    SickLeaveType.ShouldHaveWorked =>
                        LocalizationHelper.Translate("SickLeave_Description_FullDay"),

                    SickLeaveType.WouldBeFree =>
                        LocalizationHelper.Translate("SickLeave_Description_FreeDay"),

                    _ => LocalizationHelper.Translate("SickLeave_Default")
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fel i GetSickLeaveDescriptionAsync: {ex.Message}");
                return LocalizationHelper.Translate("SickLeave_Default");
            }
        }
    }
}
