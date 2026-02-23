using Microsoft.Extensions.Logging;
using MyWorkSalary.Helpers.Converters;
using MyWorkSalary.Services;
using MyWorkSalary.Services.ApiClients;
using MyWorkSalary.Services.Builders;
using MyWorkSalary.Services.Calculations;
using MyWorkSalary.Services.Conflicts;
using MyWorkSalary.Services.Handlers;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Services.Premium;
using MyWorkSalary.Services.Repositories;
using MyWorkSalary.Services.Validation;
using MyWorkSalary.ViewModels;
using MyWorkSalary.ViewModels.Settings;
using MyWorkSalary.ViewModels.ShiftTypes;
using MyWorkSalary.ViewModels.Templates;
using MyWorkSalary.Views;
using MyWorkSalary.Views.Pages;
using MyWorkSalary.Views.Pages.Templates;
using MyWorkSalary.Views.Settings;

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
            builder.Services.AddSingleton<IVacationLeaveRepository, VacationLeaveRepository>();

            // ===== OnCall / Jour =====
            builder.Services.AddTransient<IOnCallRepository, OnCallShiftRepository>();
            builder.Services.AddTransient<IOnCallCalloutRepository, OnCallCalloutRepository>();
            builder.Services.AddTransient<IOnCallRecalcService, OnCallRecalcService>();

            builder.Services.AddTransient<IAppSettingsRepository, AppSettingsRepository>();
            builder.Services.AddTransient<IOBRateRepository, OBRateRepository>();
            builder.Services.AddTransient<IFlexTimeRepository, FlexTimeRepository>();
            builder.Services.AddTransient<ISalaryRepository, SalaryRepository>();
            builder.Services.AddTransient<HolidayRepository>();
            builder.Services.AddSingleton<IOBEventRepository, OBEventRepository>();

            // Registrera nya Api
            builder.Services.AddTransient<HolidayApiClient>();

            // Registrera nya Services
            builder.Services.AddTransient<IShiftValidationService, ShiftValidationService>();
            builder.Services.AddTransient<IConflictResolutionService, ConflictResolutionService>();
            builder.Services.AddTransient<IWorkShiftService, WorkShiftService>();
            builder.Services.AddTransient<IShiftCalculationService, ShiftCalculationService>();
            builder.Services.AddTransient<IShiftBuilderService, ShiftBuilderService>();
            builder.Services.AddTransient<IConflictHandlerService, ConflictHandlerService>();
            builder.Services.AddTransient<IDashboardService, DashboardService>();

            builder.Services.AddTransient<HolidayService>();
            builder.Services.AddTransient<IOBEventService, OBEventService>();

            builder.Services.AddSingleton<IPremiumService, PremiumService>();
            builder.Services.AddSingleton<IFeatureLockService, FeatureLockService>();

            // === HANDLERS ===
            builder.Services.AddTransient<ShiftTypeHandler>();
            builder.Services.AddTransient<SickLeaveHandler>();
            builder.Services.AddTransient<OnCallHandler>();
            builder.Services.AddTransient<VacationHandler>();
            builder.Services.AddTransient<SalaryStatsHandler>();

            // === VIEWMODELS ===
            builder.Services.AddTransient<HomeViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<AddJobViewModel>();
            builder.Services.AddTransient<EditJobViewModel>();
            
            builder.Services.AddTransient<ShiftPageViewModel>();
            builder.Services.AddTransient<AddShiftViewModel>();
            builder.Services.AddTransient<SickLeaveViewModel>();
            builder.Services.AddTransient<OnCallViewModel>();
            builder.Services.AddTransient<VacationViewModel>();
            builder.Services.AddTransient<AddOBRateViewModel>();
            builder.Services.AddTransient<OBTemplatesViewModel>();
            builder.Services.AddTransient<SalaryPageViewModel>();

            builder.Services.AddTransient<AboutAppViewModel>();
            builder.Services.AddTransient<PremiumInfoViewModel>();

            // Registrera Views
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<JobSettingsPage>();
            builder.Services.AddTransient<AboutAppPage>();
            builder.Services.AddTransient<AddJobPage>();
            builder.Services.AddTransient<EditJobPage>();
            builder.Services.AddTransient<ShiftPage>();
            builder.Services.AddTransient<AddShiftPage>();
            builder.Services.AddTransient<AddOBRatePage>();
            builder.Services.AddTransient<SalaryPage>();
            builder.Services.AddTransient<OBTemplatesPage>();
            builder.Services.AddTransient<MyWorkSalary.Views.Startup.StartupPage>();

            // Premium
            builder.Services.AddTransient<PremiumInfoPage>();

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
