using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Services.Premium;

namespace MyWorkSalary.ViewModels.Settings
{
    public class PremiumInfoViewModel : BaseViewModel
    {
        private readonly IPremiumService _premiumService;

        public bool IsFreeUser => !_premiumService.IsPremium && !_premiumService.IsSubscriber;
        public bool HasPremium => _premiumService.IsPremium;
        public bool HasSubscription => _premiumService.IsSubscriber;
        public bool ShowSubscription => IsFreeUser || HasPremium;

        public string SubscriptionStartFormatted => _premiumService.SubscriptionStartDate != null
                ? LocalizationHelper.Translate("Premium_Subscription_StartDate",
                  _premiumService.SubscriptionStartDate.Value.ToString("yyyy-MM-dd"))
                : string.Empty;
        public string SubscriptionEndFormatted => _premiumService.SubscriptionEndDate != null
                ? LocalizationHelper.Translate("Premium_Subscription_EndDate",
                      _premiumService.SubscriptionEndDate.Value.ToString("yyyy-MM-dd"))
                : string.Empty;
        public bool IsConvertedPremium =>
            HasPremium && _premiumService.SubscriptionStartDate != null;

        public PremiumInfoViewModel(IPremiumService premiumService)
        {
            _premiumService = premiumService;
        }

        
    }
}
