using SQLite;
using MyWorkSalary.Models;

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
                    System.Diagnostics.Debug.WriteLine($"UPPDATERAR jobb: {jobProfile.JobTitle} (ID: {jobProfile.Id})");
                    var result = _database.Update(jobProfile);
                    System.Diagnostics.Debug.WriteLine($"Update påverkade {result} rader");
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

        // FÖR DEBUG TEST
        public void DeleteAllJobProfiles()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== STARTAR RADERING ===");

                // Använd SQL direkt - fungerar alltid
                var result = _database.Execute("DELETE FROM JobProfile");
                System.Diagnostics.Debug.WriteLine($"SQL DELETE påverkade {result} rader");

                // Kolla resultat
                var remaining = GetJobProfiles();
                System.Diagnostics.Debug.WriteLine($"Efter radering: {remaining.Count()} jobb kvar");

                System.Diagnostics.Debug.WriteLine("=== RADERING KLAR ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL vid radering: {ex.Message}");
            }
        }

        // FÖR DEBUG TEST
        public void ForceDeleteAllJobs()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== FORCE DELETE ===");

                // Använd SQL direkt
                var result = _database.Execute("DELETE FROM JobProfile");
                System.Diagnostics.Debug.WriteLine($"SQL DELETE påverkade {result} rader");

                // Kolla resultat
                var remaining = GetJobProfiles();
                System.Diagnostics.Debug.WriteLine($"Efter SQL DELETE: {remaining.Count()} jobb kvar");

                System.Diagnostics.Debug.WriteLine("=== FORCE DELETE KLAR ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL vid force delete: {ex.Message}");
            }
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

        #region WorkShift Methods
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
                return _database.Update(workShift);
            }
            else
            {
                return _database.Insert(workShift);
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
    }
}
