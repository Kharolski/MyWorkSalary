using MyWorkSalary.Views.Pages;
using MyWorkSalary.Views.Pages.Templates;

namespace MyWorkSalary
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Registrera routes för navigation
            Routing.RegisterRoute(nameof(AddJobPage), typeof(AddJobPage));
            Routing.RegisterRoute(nameof(EditJobPage), typeof(EditJobPage));
            Routing.RegisterRoute(nameof(AddShiftPage), typeof(AddShiftPage));
            Routing.RegisterRoute(nameof(AddOBRatePage), typeof(AddOBRatePage));

            // OB Templates Page
            Routing.RegisterRoute(nameof(OBTemplatesPage), typeof(OBTemplatesPage));
        }
    }
}
