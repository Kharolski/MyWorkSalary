using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Services;

namespace MyWorkSalary
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
            // Ladda språk & tema (efter DI är redo)
            LoadAndApplyLanguage();
            LoadAndApplyTheme();
        }

        private void LoadAndApplyLanguage()
        {
            try
            {
                var databaseService = Handler.MauiContext?.Services.GetService<DatabaseService>();

                if (databaseService != null)
                {
                    var appSettings = databaseService.AppSettings.GetAppSettings();

                    // 🟢 Debug före init
                    System.Diagnostics.Debug.WriteLine($"[LANG] Före init - LanguageCode: {appSettings.LanguageCode}");

                    // Initiera språk
                    LanguageInitializer.InitializeLanguage(appSettings);

                    // 🟢 Debug efter init
                    System.Diagnostics.Debug.WriteLine($"[LANG] Efter init - LanguageCode: {appSettings.LanguageCode}, Culture: {System.Globalization.CultureInfo.CurrentUICulture}");

                    // Spara så att användarens språkval alltid finns kvar
                    databaseService.AppSettings.SaveAppSettings(appSettings);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL vid språk-laddning: {ex.Message}");
            }
        }

        private void LoadAndApplyTheme()
        {
            try
            {
                var databaseService = Handler.MauiContext?.Services.GetService<DatabaseService>();

                if (databaseService != null)
                {
                    var appSettings = databaseService.AppSettings.GetAppSettings();
                    UserAppTheme = appSettings.IsDarkTheme ? AppTheme.Dark : AppTheme.Light;
                }
                else
                {
                    UserAppTheme = AppTheme.Light;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL vid tema-laddning: {ex.Message}");
                UserAppTheme = AppTheme.Light;
            }
        }
    }
}
