using Microsoft.Extensions.Logging;
using MyWorkSalary.Helpers.Converters;
using MyWorkSalary.Services;
using MyWorkSalary.Services.Builders;
using MyWorkSalary.Services.Calculations;
using MyWorkSalary.Services.Conflicts;
using MyWorkSalary.Services.Handlers;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Services.Repositories;
using MyWorkSalary.Services.Validation;
using MyWorkSalary.ViewModels;
using MyWorkSalary.ViewModels.ShiftTypes;
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
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            try
            {
                // Force SQLite initialization
                SQLitePCL.Batteries_V2.Init();

                // Registrera DatabaseService
                string dbPath = Path.Combine(FileSystem.AppDataDirectory, "WorkSalary.db3");
                builder.Services.AddSingleton<DatabaseService>(s => new DatabaseService(dbPath));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🚨 Database Error: {ex}");
            }

            // === REPOSITORIES ===
            builder.Services.AddTransient<IJobProfileRepository, JobProfileRepository>();
            builder.Services.AddTransient<IWorkShiftRepository, WorkShiftRepository>();
            builder.Services.AddTransient<ISickLeaveRepository, SickLeaveRepository>();
            builder.Services.AddSingleton<IVABLeaveRepository, VABLeaveRepository>();
            builder.Services.AddSingleton<IVacationLeaveRepository, VacationLeaveRepository>();
            builder.Services.AddSingleton<IOnCallRepository, OnCallShiftRepository>();
            builder.Services.AddTransient<IAppSettingsRepository, AppSettingsRepository>();
            builder.Services.AddTransient<IOBRateRepository, OBRateRepository>();
            builder.Services.AddTransient<IFlexTimeRepository, FlexTimeRepository>();

            // Registrera nya Services
            builder.Services.AddTransient<IShiftValidationService, ShiftValidationService>();
            builder.Services.AddTransient<IConflictResolutionService, ConflictResolutionService>();
            builder.Services.AddTransient<IWorkShiftService, WorkShiftService>();
            builder.Services.AddTransient<IShiftCalculationService, ShiftCalculationService>();
            builder.Services.AddTransient<IShiftBuilderService, ShiftBuilderService>();
            builder.Services.AddTransient<IConflictHandlerService, ConflictHandlerService>();
            builder.Services.AddTransient<IDashboardService, DashboardService>();

            // === HANDLERS ===
            builder.Services.AddTransient<ShiftTypeHandler>();
            //builder.Services.AddTransient<FlexTimeHandler>(); // Inte färdigt än
            builder.Services.AddTransient<VABHandler>();
            builder.Services.AddTransient<SickLeaveHandler>();
            builder.Services.AddTransient<OnCallHandler>();
            builder.Services.AddTransient<VacationHandler>();

            // === VIEWMODELS ===
            builder.Services.AddTransient<HomeViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<AddJobViewModel>();
            builder.Services.AddTransient<EditJobViewModel>();
            
            builder.Services.AddTransient<ShiftPageViewModel>();
            builder.Services.AddTransient<AddShiftViewModel>();
            builder.Services.AddTransient<VABViewModel>();
            builder.Services.AddTransient<SickLeaveViewModel>();
            builder.Services.AddTransient<OnCallViewModel>();
            builder.Services.AddTransient<VacationViewModel>();
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

            var app = builder.Build();

            // Initialisera converters
            InitializeConverters(app.Services);

            return app;
        }

        private static void InitializeConverters(IServiceProvider services)
        {
            try
            {
                var workShiftService = services.GetRequiredService<IWorkShiftService>();

                // Initialisera befintliga converters med DI
                ShiftToHoursDisplayConverter.Initialize(workShiftService);
                ShiftToTimeStringConverter.Initialize(workShiftService); 
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fel vid converter-initialisering: {ex.Message}");
            }
        }
    }
}
