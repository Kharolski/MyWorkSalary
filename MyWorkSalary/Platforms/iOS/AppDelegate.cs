using Foundation;
using UIKit;
using Google.MobileAds;

namespace MyWorkSalary
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            // Initialize AdMob for iOS
            MobileAds.SharedInstance.Start(completionHandler: null);
            
            return base.FinishedLaunching(application, launchOptions);
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
