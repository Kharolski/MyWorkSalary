using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;
using SQLite;

namespace MyWorkSalary.Services.Repositories
{
    public class JobProfileRepository : IJobProfileRepository
    {
        private readonly SQLiteConnection _database;

        public JobProfileRepository(DatabaseService databaseService)
        {
            _database = databaseService.GetConnection();
        }

        #region Basic CRUD

        public List<JobProfile> GetJobProfiles()
        {
            return _database.Table<JobProfile>()
                           .OrderByDescending(x => x.IsActive)
                           .ThenBy(x => x.JobTitle)
                           .ToList();
        }

        public JobProfile GetJobProfile(int id)
        {
            return _database.Table<JobProfile>()
                           .FirstOrDefault(x => x.Id == id);
        }

        public JobProfile GetActiveJob()
        {
            return _database.Table<JobProfile>()
                           .FirstOrDefault(x => x.IsActive);
        }

        public void SaveJobProfile(JobProfile jobProfile)
        {
            try
            {
                if (jobProfile.Id == 0)
                {
                    // Nytt jobb - INSERT
                    jobProfile.CreatedDate = DateTime.Now;
                    _database.Insert(jobProfile);
                }
                else
                {
                    // Befintligt jobb - UPDATE
                    jobProfile.ModifiedDate = DateTime.Now;
                    _database.Update(jobProfile);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i SaveJobProfile: {ex.Message}");
                throw;
            }
        }

        public int DeleteJobProfile(int id)
        {
            try
            {
                var job = GetJobProfile(id);
                if (job == null)
                    throw new InvalidOperationException("Jobbet finns inte");

                var totalJobs = _database.Table<JobProfile>().Count();

                // Om jobbet är aktivt och det finns fler jobb
                if (job.IsActive && totalJobs > 1)
                {
                    // Hitta ett annat jobb som kan bli aktivt
                    var newActiveJob = _database.Table<JobProfile>()
                                                .FirstOrDefault(x => x.Id != id);
                    if (newActiveJob != null)
                    {
                        newActiveJob.IsActive = true;
                        newActiveJob.ModifiedDate = DateTime.Now;
                        _database.Update(newActiveJob);
                    }
                }

                // 1. Hämta alla WorkShifts för jobbet
                var workShifts = _database.Table<WorkShift>().Where(x => x.JobProfileId == id).ToList();

                // 2. Radera alla relaterade poster för varje WorkShift
                foreach (var shift in workShifts)
                {
                    var sickLeaves = _database.Table<SickLeave>().Where(x => x.WorkShiftId == shift.Id).ToList();
                    foreach (var sickLeave in sickLeaves)
                        _database.Delete<SickLeave>(sickLeave.Id);

                    var vacationLeaves = _database.Table<VacationLeave>().Where(x => x.WorkShiftId == shift.Id).ToList();
                    foreach (var vacation in vacationLeaves)
                        _database.Delete<VacationLeave>(vacation.Id);

                    var onCallShifts = _database.Table<OnCallShift>()
                        .Where(x => x.WorkShiftId == shift.Id)
                        .ToList();

                    foreach (var onCall in onCallShifts)
                    {
                        // Radera callouts först
                        _database.Execute("DELETE FROM OnCallCallout WHERE OnCallShiftId = ?", onCall.Id);

                        // Radera jourshift
                        _database.Delete<OnCallShift>(onCall.Id);
                    }
                }

                // 3. Radera alla WorkShifts
                foreach (var shift in workShifts)
                    _database.Delete<WorkShift>(shift.Id);

                // 4. Radera OB-regler
                var obRates = _database.Table<OBRate>().Where(x => x.JobProfileId == id).ToList();
                foreach (var obRate in obRates)
                    _database.Delete<OBRate>(obRate.Id);

                // 5. Radera FlexTimeBalance
                var flexTimes = _database.Table<FlexTimeBalance>().Where(x => x.JobProfileId == id).ToList();
                foreach (var flex in flexTimes)
                    _database.Delete<FlexTimeBalance>(flex.Id);

                // 6. Slutligen radera själva jobbet
                return _database.Delete<JobProfile>(id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i DeleteJobProfile: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Business Logic

        public bool HasActiveJob()
        {
            return _database.Table<JobProfile>().Any(x => x.IsActive);
        }

        public void SetActiveJob(int jobProfileId)
        {
            // Deaktivera alla jobb först
            DeactivateAllJobs();

            // Aktivera det valda jobbet
            var job = GetJobProfile(jobProfileId);
            if (job != null)
            {
                job.IsActive = true;
                job.ModifiedDate = DateTime.Now;
                _database.Update(job);
            }
        }

        public void DeactivateAllJobs()
        {
            var activeJobs = _database.Table<JobProfile>().Where(x => x.IsActive).ToList();
            foreach (var job in activeJobs)
            {
                job.IsActive = false;
                job.ModifiedDate = DateTime.Now;
                _database.Update(job);
            }
        }

        #endregion

        #region Validation

        public bool IsJobNameUnique(string jobName, int excludeId = 0)
        {
            return !_database.Table<JobProfile>()
                            .Any(x => x.JobTitle.ToLower() == jobName.ToLower() && x.Id != excludeId);
        }

        public bool CanDeleteJob(int jobProfileId)
        {
            // Force-delete: tillåt alltid radering
            return true;
        }

        #endregion

        #region Statistics

        public int GetTotalJobsCount()
        {
            return _database.Table<JobProfile>().Count();
        }

        public List<JobProfile> GetRecentJobs(int count = 5)
        {
            return _database.Table<JobProfile>()
                           .OrderByDescending(x => x.ModifiedDate ?? x.CreatedDate)
                           .Take(count)
                           .ToList();
        }

        #endregion
    }
}
