using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;
using SQLite;

namespace MyWorkSalary.Services.Repositories
{
    public class WorkShiftRepository : IWorkShiftRepository
    {
        private readonly SQLiteConnection _database;

        public WorkShiftRepository(DatabaseService databaseService)
        {
            _database = databaseService.GetConnection();
        }

        #region Basic CRUD

        public List<WorkShift> GetWorkShifts(int jobProfileId)
        {
            return _database.Table<WorkShift>()
                           .Where(x => x.JobProfileId == jobProfileId)
                           .OrderByDescending(x => x.ShiftDate)
                           .ToList();
        }

        public List<WorkShift> GetWorkShifts(int jobProfileId, DateTime fromDate, DateTime toDateExclusive)
        {
            // Hämta på ShiftDate (enkel query som SQLite klarar)
            var fromDay = fromDate.Date.AddDays(-1);
            var toDayExclusive = toDateExclusive.Date.AddDays(1);

            var candidates = _database.Table<WorkShift>()
                .Where(x => x.JobProfileId == jobProfileId
                         && x.ShiftDate >= fromDay
                         && x.ShiftDate < toDayExclusive)
                .ToList();

            // Filtrera i minnet för exakt overlap (inkl pass som går över midnatt)
            return candidates
                .Where(s => s.StartTime.HasValue && s.EndTime.HasValue)
                .Where(s => s.EndTime.Value > fromDate && s.StartTime.Value < toDateExclusive) // overlap
                .OrderBy(s => s.StartTime)
                .ToList();
        }

        public WorkShift GetWorkShift(int id)
        {
            return _database.Table<WorkShift>()
                           .FirstOrDefault(x => x.Id == id);
        }

        public WorkShift SaveWorkShift(WorkShift workShift)
        {
            if (workShift.Id != 0)
            {
                workShift.ModifiedDate = DateTime.Now;
                var result = _database.Update(workShift);
                return workShift;
            }
            else
            {
                workShift.CreatedDate = DateTime.Now;
                var result = _database.Insert(workShift);
                return workShift;
            }
        }

        public async Task<WorkShift> SaveWorkShiftAsync(WorkShift workShift)
        {
            return await Task.Run(() => SaveWorkShift(workShift));
        }

        public List<WorkShift> GetWorkShiftsForDate(int jobProfileId, DateTime date)
        {
            return _database.Table<WorkShift>()
                   .Where(x => x.JobProfileId == jobProfileId)
                   .ToList()
                   .Where(x => x.ShiftDate.Date == date.Date)
                   .OrderBy(x => x.StartTime)
                   .ToList();
        }

        public List<WorkShift> GetWorkShiftsForDateRange(int jobProfileId, DateTime fromDate, DateTime toDate)
        {
            return GetWorkShifts(jobProfileId, fromDate, toDate);
        }

        public int DeleteWorkShift(int id)
        {
            try
            {
                // Ta bort kopplad SickLeave först (om det finns)
                var sickLeaves = _database.Table<SickLeave>().Where(x => x.WorkShiftId == id).ToList();
                foreach (var sickLeave in sickLeaves)
                {
                    _database.Delete<SickLeave>(sickLeave.Id);
                }
                // Ta bort WorkShift
                return _database.Delete<WorkShift>(id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i DeleteWorkShift: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region Queries by ShiftType

        public List<WorkShift> GetWorkShiftsByType(int jobProfileId, ShiftType shiftType)
        {
            return _database.Table<WorkShift>()
                           .Where(x => x.JobProfileId == jobProfileId && x.ShiftType == shiftType)
                           .OrderByDescending(x => x.ShiftDate)
                           .ToList();
        }

        public List<WorkShift> GetRegularShifts(int jobProfileId, DateTime fromDate, DateTime toDate)
        {
            return _database.Table<WorkShift>()
                           .Where(x => x.JobProfileId == jobProfileId &&
                                      x.ShiftType == ShiftType.Regular &&
                                      x.ShiftDate >= fromDate &&
                                      x.ShiftDate <= toDate)
                           .OrderBy(x => x.ShiftDate)
                           .ToList();
        }

        public List<WorkShift> GetSickLeaveShifts(int jobProfileId, DateTime fromDate, DateTime toDate)
        {
            return _database.Table<WorkShift>()
                           .Where(x => x.JobProfileId == jobProfileId &&
                                      x.ShiftType == ShiftType.SickLeave &&
                                      x.ShiftDate >= fromDate &&
                                      x.ShiftDate <= toDate)
                           .OrderBy(x => x.ShiftDate)
                           .ToList();
        }

        #endregion

        #region Monthly Queries

        public List<WorkShift> GetWorkShiftsForMonth(int jobProfileId, int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDateExclusive = startDate.AddMonths(1);
            return GetWorkShifts(jobProfileId, startDate, endDateExclusive);
        }

        public (decimal TotalHours, decimal TotalPay, int TotalShifts) GetMonthlyStats(int jobProfileId, int year, int month)
        {
            var shifts = GetWorkShiftsForMonth(jobProfileId, year, month);
            var totalHours = shifts.Sum(x => x.TotalHours);
            var totalPay = shifts.Sum(x => x.TotalPay);
            var totalShifts = shifts.Count;
            return (totalHours, totalPay, totalShifts);
        }

        #endregion

        #region Validation Queries

        public List<WorkShift> GetOverlappingShifts(int jobProfileId, DateTime startTime, DateTime endTime, int excludeId = 0)
        {
            return _database.Table<WorkShift>()
                           .Where(x => x.JobProfileId == jobProfileId &&
                                      x.Id != excludeId &&
                                      x.ShiftType == ShiftType.Regular &&
                                      x.StartTime.HasValue && x.EndTime.HasValue &&
                                      x.StartTime < endTime && x.EndTime > startTime)
                           .ToList();
        }

        public bool HasShiftOnDate(int jobProfileId, DateTime date, int excludeId = 0)
        {
            return _database.Table<WorkShift>()
                           .Any(x => x.JobProfileId == jobProfileId &&
                                    x.Id != excludeId &&
                                    x.ShiftDate.Date == date.Date);
        }

        #endregion

        #region Statistics

        public decimal GetTotalHoursForPeriod(int jobProfileId, DateTime fromDate, DateTime toDate)
        {
            return _database.Table<WorkShift>()
                           .Where(x => x.JobProfileId == jobProfileId &&
                                      x.ShiftDate >= fromDate &&
                                      x.ShiftDate <= toDate)
                           .Sum(x => x.TotalHours);
        }

        public decimal GetTotalPayForPeriod(int jobProfileId, DateTime fromDate, DateTime toDate)
        {
            return _database.Table<WorkShift>()
                           .Where(x => x.JobProfileId == jobProfileId &&
                                      x.ShiftDate >= fromDate &&
                                      x.ShiftDate <= toDate)
                           .Sum(x => x.TotalPay);
        }

        public int GetShiftCountForYear(int jobProfileId, int year)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);
            return _database.Table<WorkShift>()
                           .Count(x => x.JobProfileId == jobProfileId &&
                                      x.ShiftDate >= startDate &&
                                      x.ShiftDate <= endDate);
        }

        #endregion

        #region Recent Data

        public List<WorkShift> GetRecentShifts(int jobProfileId, int count = 10)
        {
            return _database.Table<WorkShift>()
                           .Where(x => x.JobProfileId == jobProfileId)
                           .OrderByDescending(x => x.ShiftDate)
                           .Take(count)
                           .ToList();
        }

        public WorkShift GetLastShift(int jobProfileId)
        {
            return _database.Table<WorkShift>()
                           .Where(x => x.JobProfileId == jobProfileId)
                           .OrderByDescending(x => x.ShiftDate)
                           .FirstOrDefault();
        }

        #endregion
    }
}
