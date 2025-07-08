using MyWorkSalary.Models.Specialized;

namespace MyWorkSalary.Services.Interfaces
{
    public interface ISickLeaveRepository
    {
        // CRUD operations
        Task<int> SaveSickLeaveAsync(SickLeave sickLeave);
        SickLeave GetSickLeaveByWorkShiftId(int workShiftId);
        SickLeave GetSickLeaveById(int id);
        int DeleteSickLeave(int id);

        // Sjukperiod-relaterade
        List<SickLeave> GetSickLeavesByPeriodId(int periodId);
        int GetNextSickPeriodId();

        // Queries för beräkningar
        List<SickLeave> GetSickLeavesForDateRange(int jobProfileId, DateTime fromDate, DateTime toDate);
        List<SickLeave> GetRecentSickLeaves(int jobProfileId, DateTime fromDate, int days = 5);

        // Statistik
        decimal GetTotalSickPayForPeriod(int jobProfileId, DateTime fromDate, DateTime toDate);
        int GetSickDaysCountForYear(int jobProfileId, int year);
    }
}
