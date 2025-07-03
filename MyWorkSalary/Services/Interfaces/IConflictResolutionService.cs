using MyWorkSalary.Models;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IConflictResolutionService
    {
        (bool HasConflict, string ConflictMessage, List<WorkShift> ConflictingShifts) GetLeaveConflictDetails(WorkShift newShift);
        (bool HasConflict, string ConflictMessage, WorkShift ConflictingSickLeave) CheckWorkShiftAgainstSickLeave(WorkShift workShift);
        Task<(bool Success, string Message)> SaveSickLeaveWithConflictResolution(WorkShift sickShift);
        Task<(bool Success, string Message)> SaveSickLeaveAndRemoveConflicts(WorkShift sickShift);
        Task<(bool Success, string Message)> ShortenSickLeave(WorkShift sickLeave, DateTime newEndDate);
        Task<(bool Success, string Message)> MergeLeavePeriods(WorkShift newShift, List<int> existingShiftIds, DateTime mergedStartDate, int mergedDays);
        Task<(bool Success, string Message)> RemoveWorkShiftAndSaveLeave(int workShiftId, WorkShift leaveShift);
        List<WorkShift> GetWorkShiftsDuringSickLeave(WorkShift sickShift);
    }
}
