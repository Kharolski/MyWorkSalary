using SQLite;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.Services.Repositories
{
    public class VABLeaveRepository : IVABLeaveRepository
    {
        private readonly DatabaseService _databaseService;
        private SQLiteConnection Database => _databaseService.GetConnection();

        public VABLeaveRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        #region CRUD Operations
        public async Task<int> InsertAsync(VABLeave vabLeave)
        {
            return await Task.Run(() => Database.Insert(vabLeave));
        }

        public async Task<int> DeleteAsync(int id)
        {
            return await Task.Run(() => Database.Delete<VABLeave>(id));
        }

        public async Task<VABLeave> GetByIdAsync(int id)
        {
            return await Task.Run(() => Database.Get<VABLeave>(id));
        }
        #endregion

        #region Query Operations
        public async Task<VABLeave> GetByWorkShiftIdAsync(int workShiftId)
        {
            return await Task.Run(() =>
                Database.Table<VABLeave>()
                    .Where(v => v.WorkShiftId == workShiftId)
                    .FirstOrDefault());
        }

        public VABLeave GetByWorkShiftId(int workShiftId)
        {
            return Database.Table<VABLeave>()
                .Where(v => v.WorkShiftId == workShiftId)
                .FirstOrDefault();
        }

        public async Task<List<VABLeave>> GetByJobProfileIdAsync(int jobProfileId)
        {
            return await Task.Run(() =>
                Database.Query<VABLeave>(@"
                    SELECT v.* FROM VABLeaves v
                    INNER JOIN WorkShifts w ON v.WorkShiftId = w.Id
                    WHERE w.JobProfileId = ?", jobProfileId));
        }

        public async Task<List<VABLeave>> GetByDateRangeAsync(int jobProfileId, DateTime startDate, DateTime endDate)
        {
            return await Task.Run(() =>
                Database.Query<VABLeave>(@"
                    SELECT v.* FROM VABLeaves v
                    INNER JOIN WorkShifts w ON v.WorkShiftId = w.Id
                    WHERE w.JobProfileId = ? AND w.ShiftDate BETWEEN ? AND ?
                    ORDER BY w.ShiftDate", jobProfileId, startDate, endDate));
        }

        public async Task<List<VABLeave>> GetAllAsync()
        {
            return await Task.Run(() => Database.Table<VABLeave>().ToList());
        }
        #endregion

        #region Business Logic Queries
        public async Task<decimal> GetTotalVABHoursAsync(int jobProfileId, DateTime startDate, DateTime endDate)
        {
            return await Task.Run(() =>
                Database.ExecuteScalar<decimal>(@"
                    SELECT COALESCE(SUM(v.VABHours), 0) FROM VABLeaves v
                    INNER JOIN WorkShifts w ON v.WorkShiftId = w.Id
                    WHERE w.JobProfileId = ? AND w.ShiftDate BETWEEN ? AND ?",
                    jobProfileId, startDate, endDate));
        }

        public async Task<decimal> GetTotalVABDeductionAsync(int jobProfileId, DateTime startDate, DateTime endDate)
        {
            return await Task.Run(() =>
                Database.ExecuteScalar<decimal>(@"
                    SELECT COALESCE(SUM(v.VABDeduction), 0) FROM VABLeaves v
                    INNER JOIN WorkShifts w ON v.WorkShiftId = w.Id
                    WHERE w.JobProfileId = ? AND w.ShiftDate BETWEEN ? AND ?",
                    jobProfileId, startDate, endDate));
        }

        public async Task<int> GetVABDaysCountAsync(int jobProfileId, DateTime startDate, DateTime endDate)
        {
            return await Task.Run(() =>
                Database.ExecuteScalar<int>(@"
                    SELECT COUNT(*) FROM VABLeaves v
                    INNER JOIN WorkShifts w ON v.WorkShiftId = w.Id
                    WHERE w.JobProfileId = ? AND w.ShiftDate BETWEEN ? AND ?",
                    jobProfileId, startDate, endDate));
        }
        #endregion

        #region Utility
        public async Task<bool> ExistsAsync(int workShiftId)
        {
            return await Task.Run(() =>
                Database.Table<VABLeave>()
                    .Where(v => v.WorkShiftId == workShiftId)
                    .Any());
        }

        public async Task<int> DeleteByJobProfileIdAsync(int jobProfileId)
        {
            return await Task.Run(() =>
                Database.Execute(@"
                    DELETE FROM VABLeaves 
                    WHERE WorkShiftId IN (
                        SELECT Id FROM WorkShifts WHERE JobProfileId = ?
                    )", jobProfileId));
        }
        #endregion
    }
}
