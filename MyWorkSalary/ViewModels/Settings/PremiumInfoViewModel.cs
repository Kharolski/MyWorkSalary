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
        public bool ShowDebugTools => true; // Always show for testing

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

        public PremiumInfoViewModel(IPremiumService premiumService)
        {
            _premiumService = premiumService;
        }

        // Demo purchase methods
        public void PurchasePremium()
        {
            _premiumService.SetPremium(true);
            OnPropertyChanged(nameof(IsFreeUser));
            OnPropertyChanged(nameof(HasPremium));
            OnPropertyChanged(nameof(ShowSubscription));
            OnPropertyChanged(nameof(PremiumDebugStatus));
        }

        public void PurchaseSubscription()
        {
            _premiumService.SetSubscription(true);
            OnPropertyChanged(nameof(IsFreeUser));
            OnPropertyChanged(nameof(HasSubscription));
            OnPropertyChanged(nameof(SubscriptionStartFormatted));
            OnPropertyChanged(nameof(ShowSubscription));
            OnPropertyChanged(nameof(PremiumDebugStatus));
        }

        public void CancelSubscription()
        {
            _premiumService.SetSubscription(false);
            OnPropertyChanged(nameof(IsFreeUser));
            OnPropertyChanged(nameof(HasSubscription));
            OnPropertyChanged(nameof(HasPremium));
            OnPropertyChanged(nameof(SubscriptionEndFormatted));
            OnPropertyChanged(nameof(ShowSubscription));
            OnPropertyChanged(nameof(PremiumDebugStatus));
        }

        public void ClearAllPremium()
        {
            _premiumService.ClearAll();
            OnPropertyChanged(nameof(IsFreeUser));
            OnPropertyChanged(nameof(HasPremium));
            OnPropertyChanged(nameof(HasSubscription));
            OnPropertyChanged(nameof(SubscriptionStartFormatted));
            OnPropertyChanged(nameof(SubscriptionEndFormatted));
            OnPropertyChanged(nameof(ShowSubscription));
            OnPropertyChanged(nameof(PremiumDebugStatus));
        }
    }
}
