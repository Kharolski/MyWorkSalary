using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IShiftValidationService
    {
        (bool IsValid, string Message) ValidateSickLeave(WorkShift sickShift);
        public (bool CanAdd, string ErrorMessage, List<WorkShift> ConflictingShifts) ValidateVacationDate(
            int jobProfileId,
            DateTime vacationDate,
            VacationType vacationType);
        //(bool IsValid, string Message) ValidateVacation(WorkShift vacationShift);
        bool HasOverlappingShift(WorkShift newShift);
        WorkShift? GetOverlappingShift(WorkShift newShift);
        bool HasConflictingLeave(WorkShift newShift);
        bool HasShiftOnDate(int jobProfileId, DateTime date, ShiftType? shiftType = null);
        public (bool HasConflict, string ConflictMessage, WorkShift ConflictingLeave) CheckWorkShiftAgainstFullDayLeave(WorkShift workShift);
    }
}
