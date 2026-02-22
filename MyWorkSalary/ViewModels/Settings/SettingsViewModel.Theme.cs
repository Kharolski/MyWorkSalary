namespace MyWorkSalary.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        #region Theme Methods
        private void LoadAppSettings()
        {
            try
            {
                _appSettings = _databaseService.AppSettings.GetAppSettings();
                _isDarkTheme = _appSettings.IsDarkTheme;

                // Hämta språk från settings eller fallback
                var savedCode = string.IsNullOrEmpty(_appSettings.LanguageCode)
                    ? "en"
                    : _appSettings.LanguageCode;

                var lang = AvailableLanguages.FirstOrDefault(l => l.Code == savedCode)
                           ?? AvailableLanguages.First();

                ApplyLanguage(lang);

                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(ThemeDescription));

                // Applicera tema direkt
                ApplyTheme(_isDarkTheme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR loading app settings: {ex.Message}");

                // Fallback till ljust tema
                _isDarkTheme = false;
                ApplyTheme(false);

                // fallback språk = engelska
                SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == "en");
            }
        }

        private async void OnThemeChanged(bool isDarkTheme)
        {
            try
            {
                _appSettings.IsDarkTheme = isDarkTheme;
                _databaseService.AppSettings.SaveAppSettings(_appSettings);
                ApplyTheme(isDarkTheme);
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert(Resources.Resx.Resources.ErrorTitle,
                    Resources.Resx.Resources.ThemeSaveErrorMessage,
                    Resources.Resx.Resources.Ok);
            }
        }

        private void ApplyTheme(bool isDarkTheme)
        {
            Application.Current.UserAppTheme = isDarkTheme ? AppTheme.Dark : AppTheme.Light;
        }

        #endregion
    }
}
