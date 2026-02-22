using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Services;

namespace MyWorkSalary
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Sätt MainPage till startup-sida direkt, inte AppShell
            MainPage = new NavigationPage(new MyWorkSalary.Views.Startup.StartupPage());
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

                    // Initiera språk
                    LanguageInitializer.InitializeLanguage(appSettings);

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
