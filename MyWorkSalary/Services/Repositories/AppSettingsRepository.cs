using MyWorkSalary.Models.Core;
using MyWorkSalary.Services.Interfaces;
using SQLite;

namespace MyWorkSalary.Services.Repositories
{
    public class AppSettingsRepository : IAppSettingsRepository
    {
        private readonly SQLiteConnection _database;

        public AppSettingsRepository(DatabaseService databaseService)
        {
            _database = databaseService.GetConnection();
        }

        public AppSettings GetAppSettings()
        {
            var settings = _database.Table<AppSettings>().FirstOrDefault();
            if (settings == null)
            {
                settings = new AppSettings
                {
                    IsDarkTheme = false,
                    Language = "sv"
                };
                _database.Insert(settings);
            }
            return settings;
        }

        public void SaveAppSettings(AppSettings settings)
        {
            try
            {
                settings.LastModified = DateTime.Now;
                if (settings.Id == 0)
                {
                    _database.Insert(settings);
                }
                else
                {
                    _database.Update(settings);
                }
                _database.Execute("PRAGMA synchronous = FULL");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i SaveAppSettings: {ex.Message}");
                throw;
            }
        }

        // Convenience methods
        public bool IsDarkTheme()
        {
            return GetAppSettings().IsDarkTheme;
        }

        public void SetDarkTheme(bool isDark)
        {
            var settings = GetAppSettings();
            settings.IsDarkTheme = isDark;
            SaveAppSettings(settings);
        }

        public string GetLanguage()
        {
            return GetAppSettings().Language;
        }

        public void SetLanguage(string language)
        {
            var settings = GetAppSettings();
            settings.Language = language;
            SaveAppSettings(settings);
        }

        public void ResetToDefaults()
        {
            var settings = GetAppSettings();
            settings.IsDarkTheme = false;
            settings.Language = "sv";
            SaveAppSettings(settings);
        }
    }
}
