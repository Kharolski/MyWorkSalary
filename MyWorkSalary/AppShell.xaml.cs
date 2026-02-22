using MyWorkSalary.Views.Pages;
using MyWorkSalary.Views.Pages.Templates;
using MyWorkSalary.Views.Settings;
using MyWorkSalary.Views.Startup;

namespace MyWorkSalary
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Registrera startup route
            Routing.RegisterRoute("StartupPage", typeof(StartupPage));

            // Registrera routes för navigation
            Routing.RegisterRoute(nameof(AddJobPage), typeof(AddJobPage));
            Routing.RegisterRoute(nameof(EditJobPage), typeof(EditJobPage));
            Routing.RegisterRoute(nameof(AddShiftPage), typeof(AddShiftPage));
            Routing.RegisterRoute(nameof(AddOBRatePage), typeof(AddOBRatePage));
            Routing.RegisterRoute(nameof(JobSettingsPage), typeof(JobSettingsPage));
            Routing.RegisterRoute(nameof(AboutAppPage), typeof(AboutAppPage));

            // Premium
            Routing.RegisterRoute(nameof(PremiumInfoPage), typeof(PremiumInfoPage));

            // OB Templates Page
            Routing.RegisterRoute(nameof(OBTemplatesPage), typeof(OBTemplatesPage));
        }
    }
}
