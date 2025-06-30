using MyWorkSalary.Views.Pages;

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
        }
    }
}
