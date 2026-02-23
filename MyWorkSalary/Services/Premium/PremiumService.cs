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
    private const string SubscriptionStartKey = "SubscriptionStartDate"; 
    private const string SubscriptionEndKey = "SubscriptionEndDate";
    #endregion

    #region Properties
    public bool IsPremium => Preferences.Get(PremiumKey, false);
    public bool IsSubscriber => Preferences.Get(SubscriptionKey, false);
    public bool IsFreeUser => !IsPremium && !IsSubscriber;

    public DateTime? SubscriptionStartDate => Preferences.ContainsKey(SubscriptionStartKey) 
        ? Preferences.Get(SubscriptionStartKey, DateTime.MinValue) 
        : null; 
    
    public DateTime? SubscriptionEndDate => Preferences.ContainsKey(SubscriptionEndKey) 
        ? Preferences.Get(SubscriptionEndKey, DateTime.MinValue) 
        : null;
    #endregion

    #region Public API
    public void SetPremium(bool value)
    {
        Preferences.Set(PremiumKey, value);

        // Premium ska aldrig försvinna
        if (value)
            Preferences.Set(SubscriptionKey, false);
    }
    public void SetSubscription(bool value)
    {
        Preferences.Set(SubscriptionKey, value);

        if (value){ 
            // Starta prenumeration
            SetSubscriptionStart(DateTime.UtcNow); 
            Preferences.Set(PremiumKey, false); // prenumeration ersätter inte premium
        } 
        else { 
            // Avsluta prenumeration
            SetSubscriptionEnd(DateTime.UtcNow); 
            ConvertSubscriptionToPremiumIfEligible(); 
        }
    }

    public void SetSubscriptionStart(DateTime date) 
    { 
        Preferences.Set(SubscriptionStartKey, date); 
        Preferences.Remove(SubscriptionEndKey); 
    }
    public void SetSubscriptionEnd(DateTime date) 
    { 
        Preferences.Set(SubscriptionEndKey, date); 
    }
    public void ConvertSubscriptionToPremiumIfEligible()
    { 
        // Om användaren redan har Premium → gör ingenting
        if (IsPremium) 
            return; 

        if (SubscriptionStartDate == null || SubscriptionEndDate == null) 
            return; 
        
        int months = ((SubscriptionEndDate.Value.Year - SubscriptionStartDate.Value.Year) * 12) + 
                     (SubscriptionEndDate.Value.Month - SubscriptionStartDate.Value.Month); 
        if (months >= 3) 
        { 
            // Ge Premium permanent
            Preferences.Set(PremiumKey, true); 
        } 
        
        // Prenumeration är avslutad
        Preferences.Set(SubscriptionKey, false); 
    }

    public void ClearAll()
    {
        Preferences.Remove(PremiumKey);
        Preferences.Remove(SubscriptionKey);
        Preferences.Remove(SubscriptionStartKey);
        Preferences.Remove(SubscriptionEndKey);
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
