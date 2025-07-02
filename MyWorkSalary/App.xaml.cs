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
            // Ladda tema Efter DI är redo
            LoadAndApplyTheme();
        }

        private void LoadAndApplyTheme()
        {
            try
            {
                var databaseService = Handler.MauiContext?.Services.GetService<DatabaseService>();

                if (databaseService != null)
                {
                    var appSettings = databaseService.GetAppSettings();
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
