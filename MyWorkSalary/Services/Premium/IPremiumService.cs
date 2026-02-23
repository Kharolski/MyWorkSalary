namespace MyWorkSalary.Services.Premium;

/// <summary>
/// Interface som definierar premium-status i appen.
/// Används för att hålla koll på om användaren är:
/// - Gratisanvändare
/// - Premium (engångsköp)
/// - Prenumerant (månad/år)
/// </summary>
public interface IPremiumService
{
    bool IsPremium { get; }
    bool IsSubscriber { get; }
    bool IsFreeUser { get; }

    DateTime? SubscriptionStartDate { get; }
    DateTime? SubscriptionEndDate { get; }

    // Sätter Premium-status (engångsköp).
    void SetPremium(bool value);

    // Sätter prenumerationsstatus.
    void SetSubscription(bool value);
    void SetSubscriptionStart(DateTime date); 
    void SetSubscriptionEnd(DateTime date); 
    void ConvertSubscriptionToPremiumIfEligible();

    // Rensar alla premium-flaggor (användbart vid debugging).
    void ClearAll();
}
