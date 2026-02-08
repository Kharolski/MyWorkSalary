using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyWorkSalary.Services.Repositories
{
    public class SalaryRepository : ISalaryRepository
    {
        private readonly SQLiteConnection _database;

        public SalaryRepository(DatabaseService databaseService)
        {
            _database = databaseService.GetConnection();
        }

        #region Shifts & WorkTime

        public IEnumerable<WorkShift> GetShiftsForPeriod(int jobId, DateTime start, DateTime endExclusive)
        {
            try
            {
                var fromDay = start.Date.AddDays(-1);
                var toDayExclusive = endExclusive.Date.AddDays(1);

                var candidates = _database.Table<WorkShift>()
                    .Where(x => x.JobProfileId == jobId
                             && x.ShiftDate >= fromDay
                             && x.ShiftDate < toDayExclusive)
                    .ToList();

                return candidates
                    .Where(s => s.StartTime.HasValue && s.EndTime.HasValue)
                    .Where(s => s.EndTime.Value > start && s.StartTime.Value < endExclusive) // overlap
                    .OrderBy(s => s.StartTime)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i GetShiftsForPeriod: {ex.Message}");
                return new List<WorkShift>();
            }
        }

        public IEnumerable<OBRate> GetObShiftsForPeriod(int jobId, DateTime start, DateTime end)
        {
            try
            {
                return _database.Table<OBRate>()
                                .Where(x => x.JobProfileId == jobId)
                                .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i GetObShiftsForPeriod: {ex.Message}");
                return new List<OBRate>();
            }
        }

        #endregion

        #region Leave

        public IEnumerable<SickLeave> GetSickLeavesForPeriod(int jobId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var sickLeaves = (from s in _database.Table<SickLeave>()
                                  join w in _database.Table<WorkShift>()
                                  on s.WorkShiftId equals w.Id
                                  where w.JobProfileId == jobId &&
                                        w.ShiftDate >= startDate && w.ShiftDate <= endDate
                                  orderby w.ShiftDate
                                  select s).ToList();

                foreach (var sickLeave in sickLeaves)
                {
                    sickLeave.WorkShift = _database.Table<WorkShift>()
                                                   .FirstOrDefault(w => w.Id == sickLeave.WorkShiftId);
                }

                return sickLeaves;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i GetSickLeavesForPeriod: {ex.Message}");
                throw;
            }
        }

        public IEnumerable<VacationLeave> GetVacationForPeriod(int jobId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var vacations = (from v in _database.Table<VacationLeave>()
                                 join w in _database.Table<WorkShift>()
                                 on v.WorkShiftId equals w.Id
                                 where w.JobProfileId == jobId &&
                                       w.ShiftDate >= startDate && w.ShiftDate <= endDate
                                 orderby w.ShiftDate
                                 select v).ToList();

                foreach (var vacation in vacations)
                {
                    vacation.WorkShift = _database.Table<WorkShift>()
                                                   .FirstOrDefault(w => w.Id == vacation.WorkShiftId);
                }

                return vacations;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i GetVacationForPeriod: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region OnCall

        public IEnumerable<OnCallShift> GetOnCallForPeriod(int jobId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var onCalls = (from o in _database.Table<OnCallShift>()
                               join w in _database.Table<WorkShift>()
                               on o.WorkShiftId equals w.Id
                               where w.JobProfileId == jobId &&
                                     w.ShiftDate >= startDate && w.ShiftDate <= endDate
                               orderby w.ShiftDate
                               select o).ToList();

                foreach (var onCall in onCalls)
                {
                    onCall.WorkShift = _database.Table<WorkShift>()
                                                .FirstOrDefault(w => w.Id == onCall.WorkShiftId);
                }

                return onCalls;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i GetOnCallForPeriod: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region JobProfile

        public JobProfile GetJobProfile(int jobId)
        {
            try
            {
                return _database.Table<JobProfile>()
                                .FirstOrDefault(x => x.Id == jobId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i GetJobProfile: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
