using Microsoft.Extensions.Logging;
using MyWorkSalary.Services;
using MyWorkSalary.Services.Builders;
using MyWorkSalary.Services.Calculations;
using MyWorkSalary.Services.Conflicts;
using MyWorkSalary.Services.Handlers;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Services.Validation;
using MyWorkSalary.ViewModels;
using MyWorkSalary.Views;
using MyWorkSalary.Views.Pages;

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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Registrera DatabaseService
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "WorkSalary.db3");
            builder.Services.AddSingleton<DatabaseService>(s => new DatabaseService(dbPath));

            // Registrera nya Services
            builder.Services.AddTransient<IShiftValidationService, ShiftValidationService>();
            builder.Services.AddTransient<IConflictResolutionService, ConflictResolutionService>();
            builder.Services.AddTransient<IWorkShiftService, WorkShiftService>();
            builder.Services.AddTransient<IShiftCalculationService, ShiftCalculationService>();
            builder.Services.AddTransient<IShiftBuilderService, ShiftBuilderService>();
            builder.Services.AddTransient<IConflictHandlerService, ConflictHandlerService>();
            builder.Services.AddTransient<IDashboardService, DashboardService>();

            // Registrera ViewModels
            builder.Services.AddTransient<HomeViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<AddJobViewModel>();
            builder.Services.AddTransient<EditJobViewModel>();
            builder.Services.AddTransient<ShiftPageViewModel>();
            builder.Services.AddTransient<AddShiftViewModel>();
            builder.Services.AddTransient<AddOBRateViewModel>();

            // Registrera Views
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<AddJobPage>();
            builder.Services.AddTransient<EditJobPage>();
            builder.Services.AddTransient<ShiftPage>();
            builder.Services.AddTransient<AddShiftPage>();
            builder.Services.AddTransient<AddOBRatePage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
