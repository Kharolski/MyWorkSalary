using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;
using System.Globalization;

namespace MyWorkSalary.Services.Validation
{
    public class ShiftValidationService : IShiftValidationService
    {
        private readonly DatabaseService _databaseService;

        public ShiftValidationService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Kontrollera om semester kan läggas på valt datum
        /// </summary>
        public (bool CanAdd, string ErrorMessage, List<WorkShift> ConflictingShifts) ValidateVacationDate(
            int jobProfileId,
            DateTime vacationDate,
            VacationType vacationType)
        {
            var conflictingShifts = new List<WorkShift>();
            var existingShifts = _databaseService.WorkShifts.GetWorkShifts(jobProfileId)
                .Where(x => x.ShiftDate.Date == vacationDate.Date)
                .ToList();

            if (!existingShifts.Any())
                return (true, "", conflictingShifts);

            var culture = new CultureInfo("sv-SE");
            var dateText = vacationDate.ToString("dddd d MMMM", culture);

            foreach (var existing in existingShifts)
            {
                switch (existing.ShiftType)
                {
                    case ShiftType.Vacation:
                        conflictingShifts.Add(existing);
                        return (false,
                            LocalizationHelper.Translate("ShiftValidation_Vacation_AlreadyExists") + "\n\n" +
                            LocalizationHelper.Translate("ShiftValidation_Date", dateText) + "\n" +
                            "🏖️ " + LocalizationHelper.Translate("Shift_Vacation") + "\n\n" +
                            LocalizationHelper.Translate("ShiftValidation_Vacation_RemoveExisting"),
                            conflictingShifts);

                    case ShiftType.SickLeave:
                        conflictingShifts.Add(existing);
                        return (false,
                            LocalizationHelper.Translate("ShiftValidation_SickLeave_Exists") + "\n\n" +
                            LocalizationHelper.Translate("ShiftValidation_Date", dateText) + "\n" +
                            "🤒 " + LocalizationHelper.Translate(
                                "ShiftValidation_SickLeave_Info",
                                existing.NumberOfDays ?? 1) + "\n\n" +
                            LocalizationHelper.Translate("ShiftValidation_SickLeave_Remove"),
                            conflictingShifts);

                    case ShiftType.Regular:
                    case ShiftType.OnCall:
                        conflictingShifts.Add(existing);

                        var shiftInfo = existing.StartTime.HasValue
                            ? LocalizationHelper.Translate(
                                "ShiftValidation_WorkShift_Time",
                                existing.StartTime?.ToString("HH:mm"),
                                existing.EndTime?.ToString("HH:mm"))
                            : LocalizationHelper.Translate("ShiftValidation_WorkShift_Generic");

                        return (false,
                            LocalizationHelper.Translate("ShiftValidation_WorkShift_Exists") + "\n\n" +
                            LocalizationHelper.Translate("ShiftValidation_Date", dateText) + "\n" +
                            "💼 " + shiftInfo + "\n\n" +
                            LocalizationHelper.Translate("ShiftValidation_WorkShift_Remove"),
                            conflictingShifts);
                }
            }

            return (true, "", conflictingShifts);
        }

        // Returnera överlappande pass med säker null-hantering
        public WorkShift? GetOverlappingShift(WorkShift newShift)
        {
            // SKIPPA kontrol för semester/sjuk
            if (newShift.ShiftType == ShiftType.Vacation ||
                newShift.ShiftType == ShiftType.SickLeave ||
                !newShift.StartTime.HasValue ||
                !newShift.EndTime.HasValue)
            {
                return null;
            }

            var existingShifts = _databaseService.WorkShifts.GetWorkShifts(newShift.JobProfileId);

            foreach (var existing in existingShifts)
            {
                // Skippa om vi uppdaterar samma pass
                if (existing.Id == newShift.Id)
                    continue;

                // SKIPPA befintliga pass utan tider
                if (!existing.StartTime.HasValue || !existing.EndTime.HasValue)
                    continue;

                // Kontrollera överlapp mellan tidsperioder
                if (newShift.StartTime < existing.EndTime &&
                    newShift.EndTime > existing.StartTime)
                {
                    return existing;
                }
            }

            return null;
        }

        // Kontrollera arbetspass mot hela dagen sjuk/semester
        public (bool HasConflict, string ConflictMessage, WorkShift ConflictingLeave)
            CheckWorkShiftAgainstFullDayLeave(WorkShift workShift)
        {
            if (workShift.ShiftType == ShiftType.SickLeave ||
                workShift.ShiftType == ShiftType.Vacation ||
                !workShift.StartTime.HasValue)
                return (false, "", null);

            var workDate = workShift.StartTime.Value.Date;
            var existingShifts = _databaseService.WorkShifts.GetWorkShifts(workShift.JobProfileId);
            var culture = new CultureInfo("sv-SE");

            foreach (var existing in existingShifts)
            {
                if (existing.Id == workShift.Id)
                    continue;

                if (existing.ShiftType == ShiftType.SickLeave)
                {
                    var sickStart = existing.ShiftDate.Date;
                    var sickEnd = sickStart.AddDays((existing.NumberOfDays ?? 1) - 1);

                    if (workDate >= sickStart && workDate <= sickEnd)
                    {
                        return (true,
                            LocalizationHelper.Translate("ShiftValidation_FullDay_Sick_Title") + "\n\n" +
                            LocalizationHelper.Translate(
                                "ShiftValidation_Period",
                                sickStart.ToString("d MMM", culture),
                                sickEnd.ToString("d MMM", culture)) + "\n" +
                            LocalizationHelper.Translate(
                                "ShiftValidation_Days",
                                existing.NumberOfDays ?? 1) + "\n\n" +
                            LocalizationHelper.Translate("ShiftValidation_FullDay_Sick_CannotWork") + "\n" +
                            LocalizationHelper.Translate("ShiftValidation_FullDay_Sick_Shorten"),
                            existing);
                    }
                }

                if (existing.ShiftType == ShiftType.Vacation)
                {
                    var vacationStart = existing.ShiftDate.Date;
                    var vacationEnd = vacationStart.AddDays((existing.NumberOfDays ?? 1) - 1);

                    if (workDate >= vacationStart && workDate <= vacationEnd)
                    {
                        return (true,
                            LocalizationHelper.Translate("ShiftValidation_FullDay_Vacation_Title") + "\n\n" +
                            LocalizationHelper.Translate(
                                "ShiftValidation_Period",
                                vacationStart.ToString("d MMM", culture),
                                vacationEnd.ToString("d MMM", culture)) + "\n" +
                            LocalizationHelper.Translate(
                                "ShiftValidation_Days",
                                existing.NumberOfDays ?? 1) + "\n\n" +
                            LocalizationHelper.Translate("ShiftValidation_FullDay_Vacation_CannotWork") + "\n" +
                            LocalizationHelper.Translate("ShiftValidation_FullDay_Vacation_Shorten"),
                            existing);
                    }
                }
            }

            return (false, "", null);
        }
    }
}
