using MyWorkSalary.Views.Pages;
using MyWorkSalary.Views.Pages.Templates;
using MyWorkSalary.Views.Settings;
using MyWorkSalary.Views.Startup;

namespace MyWorkSalary
{
    public partial class AppShell : Shell
    {
        private ActivityIndicator _globalLoader;

        public AppShell()
        {
            InitializeComponent();

            // Skapa overlay för navigation feedback
            var navigationOverlay = new Grid
            {
                BackgroundColor = Colors.Transparent,
                Children =
                {
                    new ActivityIndicator
                    {
                        IsRunning = false,
                        Color = Colors.Orange,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                }
            };
            // Lägg till overlay som en "flyout" som är dold
            this.FlyoutHeader = navigationOverlay;

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

        protected override void OnNavigating(ShellNavigatingEventArgs e)
        {
            // Hitta activity indicator och sätt IsRunning = true
            if (this.FlyoutHeader is Grid grid &&
                grid.Children.FirstOrDefault() is ActivityIndicator indicator)
            {
                indicator.IsRunning = true;
            }
            base.OnNavigating(e);
        }

        protected override void OnNavigated(ShellNavigatedEventArgs e)
        {
            // Hitta activity indicator och sätt IsRunning = false
            if (this.FlyoutHeader is Grid grid &&
                grid.Children.FirstOrDefault() is ActivityIndicator indicator)
            {
                indicator.IsRunning = false;
            }
            base.OnNavigated(e);
        }
    }
}
