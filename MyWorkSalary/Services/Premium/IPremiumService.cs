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

    /// <summary>
    /// Sätter Premium-status (engångsköp).
    /// </summary>
    void SetPremium(bool value);

    /// <summary>
    /// Sätter prenumerationsstatus.
    /// </summary>
    void SetSubscription(bool value);

    /// <summary>
    /// Rensar alla premium-flaggor (användbart vid debugging).
    /// </summary>
    void ClearAll();
}
