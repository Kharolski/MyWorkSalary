using Microsoft.Extensions.Logging;
using MyWorkSalary.Services;
using MyWorkSalary.ViewModels;
using MyWorkSalary.Views.Pages;
using MyWorkSalary.Views;

namespace MyWorkSalary
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Registrera DatabaseService
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "WorkSalary.db3");
            builder.Services.AddSingleton<DatabaseService>(s => new DatabaseService(dbPath));

            // Registrera ViewModels
            builder.Services.AddTransient<HomeViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<AddJobViewModel>();
            builder.Services.AddTransient<EditJobViewModel>();

            // Registrera Views
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<AddJobPage>();
            builder.Services.AddTransient<EditJobPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
