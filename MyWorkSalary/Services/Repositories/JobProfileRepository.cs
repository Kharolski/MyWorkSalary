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
                // 1. Kontrollera om jobbet kan raderas
                if (!CanDeleteJob(id))
                {
                    throw new InvalidOperationException("Kan inte radera jobb som har registrerade pass eller är aktivt");
                }

                // 2. Hämta alla WorkShifts för detta jobb
                var workShifts = _database.Table<WorkShift>().Where(x => x.JobProfileId == id).ToList();

                // 3. För varje WorkShift - radera alla relaterade poster
                foreach (var shift in workShifts)
                {
                    // Radera SickLeaves
                    var sickLeaves = _database.Table<SickLeave>().Where(x => x.WorkShiftId == shift.Id).ToList();
                    foreach (var sickLeave in sickLeaves)
                    {
                        _database.Delete<SickLeave>(sickLeave.Id);
                    }

                    // Radera VacationLeaves
                    var vacationLeaves = _database.Table<VacationLeave>().Where(x => x.WorkShiftId == shift.Id).ToList();
                    foreach (var vacation in vacationLeaves)
                    {
                        _database.Delete<VacationLeave>(vacation.Id);
                    }

                    // Radera OnCallShifts
                    var onCallShifts = _database.Table<OnCallShift>().Where(x => x.WorkShiftId == shift.Id).ToList();
                    foreach (var onCall in onCallShifts)
                    {
                        _database.Delete<OnCallShift>(onCall.Id);
                    }

                    // Radera VABLeaves
                    var vabLeaves = _database.Table<VABLeave>().Where(x => x.WorkShiftId == shift.Id).ToList();
                    foreach (var vab in vabLeaves)
                    {
                        _database.Delete<VABLeave>(vab.Id);
                    }
                }

                // 4. Radera alla WorkShifts
                foreach (var shift in workShifts)
                {
                    _database.Delete<WorkShift>(shift.Id);
                }

                // 5. Radera OB-regler för detta jobb
                var obRates = _database.Table<OBRate>().Where(x => x.JobProfileId == id).ToList();
                foreach (var obRate in obRates)
                {
                    _database.Delete<OBRate>(obRate.Id);
                }

                // 6. Radera FlexTimeBalance för detta jobb
                var flexTimes = _database.Table<FlexTimeBalance>().Where(x => x.JobProfileId == id).ToList();
                foreach (var flex in flexTimes)
                {
                    _database.Delete<FlexTimeBalance>(flex.Id);
                }

                // 7. Slutligen radera jobbet själv
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
            // Hämta jobbet
            var job = GetJobProfile(jobProfileId);
            if (job == null)
                return false;

            // Räkna totalt antal jobb
            var totalJobs = _database.Table<JobProfile>().Count();

            // Om det är sista jobbet - tillåt radering även om det är aktivt
            if (totalJobs == 1)
            {
                // Kan bara radera om det inte har pass
                var hasShifts = _database.Table<WorkShift>().Any(x => x.JobProfileId == jobProfileId);
                return !hasShifts;
            }

            // Om det finns flera jobb - kan inte radera aktivt jobb
            if (job.IsActive)
                return false;

            // Kan inte radera jobb som har registrerade pass
            var hasWorkShifts = _database.Table<WorkShift>().Any(x => x.JobProfileId == jobProfileId);
            return !hasWorkShifts;
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
