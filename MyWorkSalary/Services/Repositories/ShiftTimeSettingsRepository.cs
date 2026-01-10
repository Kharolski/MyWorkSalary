using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;
using SQLite;

namespace MyWorkSalary.Services.Repositories
{
    public class ShiftTimeSettingsRepository : IShiftTimeSettingsRepository
    {
        private readonly SQLiteConnection _database;

        public ShiftTimeSettingsRepository(DatabaseService databaseService)
        {
            _database = databaseService.GetConnection();
            _database.CreateTable<ShiftTimeSettings>();
        }

        public ShiftTimeSettings GetForJob(int jobId)
        {
            var settings = _database.Table<ShiftTimeSettings>()
                                    .FirstOrDefault(s => s.JobProfileId == jobId);

            if (settings == null)
            {
                // Om inga inställningar finns – skapa med default
                settings = new ShiftTimeSettings
                {
                    JobProfileId = jobId
                };
                _database.Insert(settings);
            }

            return settings;
        }

        public void Save(ShiftTimeSettings settings)
        {
            settings.ModifiedDate = DateTime.Now;

            if (settings.Id == 0)
            {
                _database.Insert(settings);
            }
            else
            {
                _database.Update(settings);
            }

            // Säkerställ att ändringar skrivs till disk direkt
            _database.Execute("PRAGMA synchronous = FULL");
        }
    }
}
