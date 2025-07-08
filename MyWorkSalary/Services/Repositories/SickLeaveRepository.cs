using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;
using SQLite;

namespace MyWorkSalary.Services.Repositories
{
    public class SickLeaveRepository : ISickLeaveRepository
    {
        private readonly SQLiteConnection _database;

        public SickLeaveRepository(DatabaseService databaseService)
        {
            _database = databaseService.GetConnection();
        }

        public async Task<int> SaveSickLeaveAsync(SickLeave sickLeave)
        {
            if (sickLeave.Id == 0)
            {
                sickLeave.CreatedDate = DateTime.Now;
                return _database.Insert(sickLeave);
            }
            else
            {
                sickLeave.ModifiedDate = DateTime.Now;
                return _database.Update(sickLeave);
            }
        }

        public SickLeave GetSickLeaveByWorkShiftId(int workShiftId)
        {
            return _database.Table<SickLeave>()
                           .FirstOrDefault(x => x.WorkShiftId == workShiftId);
        }

        public SickLeave GetSickLeaveById(int id)
        {
            return _database.Table<SickLeave>()
                           .FirstOrDefault(x => x.Id == id);
        }

        public List<SickLeave> GetSickLeavesByPeriodId(int periodId)
        {
            return _database.Table<SickLeave>()
                           .Where(x => x.SickPeriodId == periodId)
                           .OrderBy(x => x.CreatedDate)
                           .ToList();
        }

        public int GetNextSickPeriodId()
        {
            var maxId = _database.Table<SickLeave>()
                               .Select(x => x.SickPeriodId ?? 0)
                               .DefaultIfEmpty(0)
                               .Max();
            return maxId + 1;
        }

        public List<SickLeave> GetRecentSickLeaves(int jobProfileId, DateTime fromDate, int days = 5)
        {
            var endDate = fromDate.AddDays(days);
            return _database.Query<SickLeave>(@"
                SELECT sl.* FROM SickLeave sl
                INNER JOIN WorkShift ws ON sl.WorkShiftId = ws.Id
                WHERE ws.JobProfileId = ? AND ws.ShiftDate >= ? AND ws.ShiftDate <= ?
                ORDER BY ws.ShiftDate DESC", jobProfileId, fromDate, endDate);
        }

        public List<SickLeave> GetSickLeavesForDateRange(int jobProfileId, DateTime fromDate, DateTime toDate)
        {
            return _database.Query<SickLeave>(@"
                SELECT sl.* FROM SickLeave sl
                INNER JOIN WorkShift ws ON sl.WorkShiftId = ws.Id
                WHERE ws.JobProfileId = ? AND ws.ShiftDate >= ? AND ws.ShiftDate <= ?
                ORDER BY ws.ShiftDate", jobProfileId, fromDate, toDate);
        }

        public decimal GetTotalSickPayForPeriod(int jobProfileId, DateTime fromDate, DateTime toDate)
        {
            var sickLeaves = GetSickLeavesForDateRange(jobProfileId, fromDate, toDate);
            return sickLeaves.Sum(x => x.DailySickEarnings ?? 0);
        }

        public int GetSickDaysCountForYear(int jobProfileId, int year)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);
            return GetSickLeavesForDateRange(jobProfileId, startDate, endDate).Count;
        }

        public int DeleteSickLeave(int id)
        {
            return _database.Delete<SickLeave>(id);
        }
    }
}
