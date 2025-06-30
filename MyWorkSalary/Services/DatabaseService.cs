using SQLite;
using MyWorkSalary.Models;
using System.Globalization;

namespace MyWorkSalary.Services
{
    public class DatabaseService
    {
        #region Private Fields
        private SQLiteConnection _database;
        #endregion

        #region Constructor
        public DatabaseService(string dbPath)
        {
            _database = new SQLiteConnection(dbPath);

            // Skapa tabeller automatiskt
            _database.CreateTable<JobProfile>();
            _database.CreateTable<OBRate>();
            _database.CreateTable<WorkShift>();
        }
        #endregion

        #region JobProfile Methods
        public List<JobProfile> GetJobProfiles()
        {
            return _database.Table<JobProfile>().ToList();
        }

        public JobProfile GetJobProfile(int id)
        {
            return _database.Table<JobProfile>().FirstOrDefault(x => x.Id == id);
        }

        public void SaveJobProfile(JobProfile jobProfile)
        {
            try
            {
                if (jobProfile.Id == 0)
                {
                    // Nytt jobb - INSERT
                    _database.Insert(jobProfile);
                }
                else
                {
                    // Befintligt jobb - UPDATE
                    var result = _database.Update(jobProfile);
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
            return _database.Delete<JobProfile>(id);
        }

        #endregion

        #region OBRate Methods
        public List<OBRate> GetOBRates(int jobProfileId)
        {
            return _database.Table<OBRate>()
                           .Where(x => x.JobProfileId == jobProfileId)
                           .ToList();
        }

        public int SaveOBRate(OBRate obRate)
        {
            return _database.InsertOrReplace(obRate);
        }

        public int DeleteOBRate(int id)
        {
            return _database.Delete<OBRate>(id);
        }
        #endregion

        #region Shift Methods
        public List<WorkShift> GetWorkShifts(int jobProfileId)
        {
            return _database.Table<WorkShift>()
                           .Where(x => x.JobProfileId == jobProfileId)
                           .ToList();
        }

        public List<WorkShift> GetWorkShifts(int jobProfileId, DateTime fromDate, DateTime toDate)
        {
            return _database.Table<WorkShift>()
                           .Where(x => x.JobProfileId == jobProfileId &&
                                      x.StartTime >= fromDate &&
                                      x.StartTime <= toDate)
                           .ToList();
        }

        public int SaveWorkShift(WorkShift workShift)
        {
            if (workShift.Id != 0)
            {
                workShift.ModifiedDate = DateTime.Now;
                var result = _database.Update(workShift);
                return result;
            }
            else
            {
                var result = _database.Insert(workShift);
                return result;
            }
        }

        public async Task<(bool Success, string Message)> SaveWorkShiftWithValidation(WorkShift workShift)
        {
            // Kontrollera överlapp
            var overlappingShift = GetOverlappingShift(workShift);
            if (overlappingShift != null)
            {
                var swedishCulture = new System.Globalization.CultureInfo("sv-SE");
                var message = $"Passet överlappar med befintligt pass:\n\n" +
                             $"📅 {overlappingShift.StartTime.ToString("dddd d MMMM", swedishCulture)}\n" +
                             $"🕐 {overlappingShift.StartTime:HH:mm} → {overlappingShift.EndTime:HH:mm}\n\n" +
                             $"Ändra tiden för att undvika överlapp.";

                return (false, message);
            }

            // Spara om ingen överlapp
            try
            {
                SaveWorkShift(workShift);
                return (true, "Passet har sparats!");
            }
            catch (Exception ex)
            {
                return (false, $"Fel vid sparande: {ex.Message}");
            }
        }

        public int DeleteWorkShift(int id)
        {
            return _database.Delete<WorkShift>(id);
        }

        #endregion

        #region Database Management
        public void CloseConnection()
        {
            _database?.Close();
        }

        public void DeleteAllData()
        {
            _database.DeleteAll<WorkShift>();
            _database.DeleteAll<OBRate>();
            _database.DeleteAll<JobProfile>();
        }
        #endregion

        #region Validering
        public bool HasOverlappingShift(WorkShift newShift)
        {
            var existingShifts = GetWorkShifts(newShift.JobProfileId);

            foreach (var existing in existingShifts)
            {
                // Skippa om vi uppdaterar samma pass
                if (existing.Id == newShift.Id)
                    continue;

                // Kontrollera överlapp
                if (newShift.StartTime < existing.EndTime && newShift.EndTime > existing.StartTime)
                {
                    return true;
                }
            }

            return false;
        }

        public WorkShift? GetOverlappingShift(WorkShift newShift)
        {
            var existingShifts = GetWorkShifts(newShift.JobProfileId);

            foreach (var existing in existingShifts)
            {
                // Skippa om vi uppdaterar samma pass
                if (existing.Id == newShift.Id)
                    continue;

                // Kontrollera överlapp
                if (newShift.StartTime < existing.EndTime && newShift.EndTime > existing.StartTime)
                {
                    return existing;
                }
            }

            return null;
        }

        #endregion
    }
}
