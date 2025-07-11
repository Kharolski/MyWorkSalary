using SQLite;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.Services.Repositories
{
    public class VacationLeaveRepository : IVacationLeaveRepository
    {
        private readonly DatabaseService _databaseService;
        private SQLiteConnection Database => _databaseService.GetConnection();

        public VacationLeaveRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        #region Basic CRUD Operations

        public async Task<VacationLeave> GetByIdAsync(int id)
        {
            return await Task.Run(() =>
                Database.Table<VacationLeave>()
                    .Where(v => v.Id == id)
                    .FirstOrDefault());
        }

        public async Task<int> InsertAsync(VacationLeave vacationLeave)
        {
            return await Task.Run(() => Database.Insert(vacationLeave));
        }

        public async Task<int> UpdateAsync(VacationLeave vacationLeave)
        {
            return await Task.Run(() => Database.Update(vacationLeave));
        }

        public async Task<int> DeleteAsync(int id)
        {
            return await Task.Run(() => Database.Delete<VacationLeave>(id));
        }

        #endregion

        #region Vacation Queries - Endast det som behövs

        /// <summary>
        /// Hämta alla semestrar för ett jobb och år (för rapporter)
        /// </summary>
        public async Task<List<VacationLeave>> GetByJobProfileAndYearAsync(int jobProfileId, int year)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);

            return await Task.Run(() =>
                Database.Query<VacationLeave>(@"
                    SELECT vl.* FROM VacationLeave vl
                    INNER JOIN WorkShift ws ON vl.WorkShiftId = ws.Id
                    WHERE ws.JobProfileId = ? AND ws.ShiftDate BETWEEN ? AND ?
                    ORDER BY ws.ShiftDate", jobProfileId, startDate, endDate));
        }

        /// <summary>
        /// Räkna använda semesterdagar (för validering)
        /// </summary>
        public async Task<decimal> GetTotalVacationDaysUsedAsync(int jobProfileId, int year)
        {
            try
            {
                var vacations = await GetByJobProfileAndYearAsync(jobProfileId, year);

                // Räkna bara betalda semesterdagar
                return vacations
                    .Where(v => v.VacationType == Models.Enums.VacationType.PaidVacation)
                    .Sum(v => v.VacationDaysConsumed);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fel i GetTotalVacationDaysUsedAsync: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Hämta alla semestrar för ett jobb (för översikt)
        /// </summary>
        public async Task<List<VacationLeave>> GetByJobProfileAsync(int jobProfileId)
        {
            return await Task.Run(() =>
                Database.Query<VacationLeave>(@"
                    SELECT vl.* FROM VacationLeave vl
                    INNER JOIN WorkShift ws ON vl.WorkShiftId = ws.Id
                    WHERE ws.JobProfileId = ?
                    ORDER BY ws.ShiftDate DESC", jobProfileId));
        }

        #endregion
    }
}
