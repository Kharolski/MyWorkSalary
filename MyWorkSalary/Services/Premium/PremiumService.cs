using Microsoft.Maui.Storage;

namespace MyWorkSalary.Services.Premium;

/// <summary>
/// Hanterar premium-status i appen.
/// Lagrar information i Preferences så att statusen finns kvar
/// även efter att appen startas om.
/// </summary>
public class PremiumService : IPremiumService
{
    #region Keys
    private const string PremiumKey = "IsPremium";
    private const string SubscriptionKey = "IsSubscriber";
    #endregion

    #region Properties
    public bool IsPremium => Preferences.Get(PremiumKey, false);
    public bool IsSubscriber => Preferences.Get(SubscriptionKey, false);
    public bool IsFreeUser => !IsPremium && !IsSubscriber;
    #endregion

    #region Public API
    public void SetPremium(bool value)
    {
        Preferences.Set(PremiumKey, value);

        if (value)
            Preferences.Set(SubscriptionKey, false);
    }
    public void SetSubscription(bool value)
    {
        Preferences.Set(SubscriptionKey, value);

        if (value)
            Preferences.Set(PremiumKey, false);
    }

    public void ClearAll()
    {
        Preferences.Remove(PremiumKey);
        Preferences.Remove(SubscriptionKey);
    }
    #endregion

    #region Future Expansion 
    // Här kommer: 
    // - Events 
    // - Notify UI 
    // - RestorePurchases 
    // - Sync with BillingService 
    // - Logging
    #endregion
}
