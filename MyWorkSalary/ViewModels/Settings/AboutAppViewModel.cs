using MyWorkSalary.Services.Premium;

namespace MyWorkSalary.ViewModels.Settings
{
    public partial class AboutAppViewModel : BaseViewModel
    {
        private readonly IPremiumService _premiumService;
        public string AppVersion => $"Version {AppInfo.VersionString}";

        public AboutAppViewModel(IPremiumService premiumService) 
        { 
            _premiumService = premiumService; 
        }

        #region Premium Debug Status
        public string PremiumDebugStatus
        {
            get
            {
                if (_premiumService.IsPremium)
                    return "⭐ Premium aktiv";

                if (_premiumService.IsSubscriber)
                {
                    var start = _premiumService.SubscriptionStartDate?.ToString("yyyy-MM-dd") ?? "-";
                    var end = _premiumService.SubscriptionEndDate?.ToString("yyyy-MM-dd") ?? "-";

                    return $"🔔 Prenumeration aktiv\nStart: {start}\nSlut: {end}";
                }

                return "🆓 Free User";
            }
        }

        public void RaisePremiumDebugStatusChanged() 
        { 
            OnPropertyChanged(nameof(PremiumDebugStatus)); 
        }
        #endregion
    }
}
