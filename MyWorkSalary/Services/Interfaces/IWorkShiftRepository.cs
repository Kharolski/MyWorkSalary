using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IWorkShiftRepository
    {
        // Basic CRUD
        List<WorkShift> GetWorkShifts(int jobProfileId);
        List<WorkShift> GetWorkShifts(int jobProfileId, DateTime fromDate, DateTime toDate);
        WorkShift GetWorkShift(int id);
        WorkShift SaveWorkShift(WorkShift workShift);
        int DeleteWorkShift(int id);

        // Queries by ShiftType
        List<WorkShift> GetWorkShiftsByType(int jobProfileId, ShiftType shiftType);
        List<WorkShift> GetRegularShifts(int jobProfileId, DateTime fromDate, DateTime toDate);
        List<WorkShift> GetSickLeaveShifts(int jobProfileId, DateTime fromDate, DateTime toDate);

        // Monthly queries
        List<WorkShift> GetWorkShiftsForMonth(int jobProfileId, int year, int month);
        (decimal TotalHours, decimal TotalPay, int TotalShifts) GetMonthlyStats(int jobProfileId, int year, int month);

        // Validation queries
        List<WorkShift> GetOverlappingShifts(int jobProfileId, DateTime startTime, DateTime endTime, int excludeId = 0);
        bool HasShiftOnDate(int jobProfileId, DateTime date, int excludeId = 0);

        // Statistics
        decimal GetTotalHoursForPeriod(int jobProfileId, DateTime fromDate, DateTime toDate);
        decimal GetTotalPayForPeriod(int jobProfileId, DateTime fromDate, DateTime toDate);
        int GetShiftCountForYear(int jobProfileId, int year);

        // Recent data
        List<WorkShift> GetRecentShifts(int jobProfileId, int count = 10);
        WorkShift GetLastShift(int jobProfileId);
    }
}
