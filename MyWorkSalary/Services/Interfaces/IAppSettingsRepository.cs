using MyWorkSalary.Models.Core;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IAppSettingsRepository
    {
        // Basic operations
        AppSettings GetAppSettings();
        void SaveAppSettings(AppSettings settings);

        // Convenience methods
        bool IsDarkTheme();
        void SetDarkTheme(bool isDark);
        string GetLanguage();
        void SetLanguage(string languageCode);

        // Reset
        void ResetToDefaults();
    }
}
