using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;
using SQLite;

namespace MyWorkSalary.Services.Repositories
{
    public class OBRateRepository : IOBRateRepository
    {
        private readonly SQLiteConnection _database;

        public OBRateRepository(DatabaseService databaseService)
        {
            _database = databaseService.GetConnection();
        }

        #region Basic CRUD

        public List<OBRate> GetOBRates(int jobProfileId)
        {
            return _database.Table<OBRate>()
                           .Where(x => x.JobProfileId == jobProfileId)
                           .OrderBy(x => x.StartTime)
                           .ToList();
        }

        public OBRate GetOBRate(int id)
        {
            return _database.Table<OBRate>()
                           .FirstOrDefault(x => x.Id == id);
        }

        public int SaveOBRate(OBRate obRate)
        {
            try
            {
                if (obRate.Id != 0)
                {
                    return _database.Update(obRate);
                }
                else
                {
                    obRate.CreatedDate = DateTime.Now;
                    return _database.Insert(obRate);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i SaveOBRate: {ex.Message}");
                throw;
            }
        }

        public int DeleteOBRate(int id)
        {
            try
            {
                return _database.Delete<OBRate>(id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i DeleteOBRate: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Business Queries

        public List<OBRate> GetActiveOBRates(int jobProfileId)
        {
            return _database.Table<OBRate>()
                           .Where(x => x.JobProfileId == jobProfileId && x.IsActive)
                           .OrderBy(x => x.StartTime)
                           .ToList();
        }

        public OBRate GetOBRateForTime(int jobProfileId, TimeSpan time, DayOfWeek dayOfWeek)
        {
            return _database.Table<OBRate>()
                           .Where(x => x.JobProfileId == jobProfileId &&
                                      x.IsActive &&
                                      x.StartTime <= time &&
                                      x.EndTime > time)
                           .FirstOrDefault();
        }

        #endregion

        #region Validation

        public bool HasOverlappingRates(OBRate obRate)
        {
            return _database.Table<OBRate>()
                           .Any(x => x.JobProfileId == obRate.JobProfileId &&
                                    x.Id != obRate.Id &&
                                    x.IsActive &&
                                    x.StartTime < obRate.EndTime &&
                                    x.EndTime > obRate.StartTime);
        }

        public bool CanDeleteOBRate(int id)
        {
            // Lägg till logik för att kontrollera om OB-regeln används
            // t.ex. om det finns WorkShifts som refererar till denna regel
            return true; // Förenklad implementation
        }

        #endregion

        #region Bulk Operations

        public void DeleteAllOBRates(int jobProfileId)
        {
            var obRates = GetOBRates(jobProfileId);
            foreach (var obRate in obRates)
            {
                DeleteOBRate(obRate.Id);
            }
        }

        public void SaveMultipleOBRates(List<OBRate> obRates)
        {
            try
            {
                foreach (var obRate in obRates)
                {
                    SaveOBRate(obRate);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i SaveMultipleOBRates: {ex.Message}");
                throw;
            }
        }

        #endregion

    }
}
