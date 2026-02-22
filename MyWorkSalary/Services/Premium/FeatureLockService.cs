namespace MyWorkSalary.Services.Premium;

/// <summary>
/// Hanterar vilka funktioner som är tillgängliga beroende på
/// om användaren är gratis, premium eller prenumerant.
/// </summary>
public class FeatureLockService : IFeatureLockService
{
    private readonly IPremiumService _premium;

    public FeatureLockService(IPremiumService premiumService)
    {
        _premium = premiumService;
    }

    #region Job Limits
    /// <summary>
    /// Gratisanvändare får bara ha 1 jobb.
    /// Premium och prenumeranter får ha obegränsat.
    /// </summary>
    public bool CanAddMoreJobs(int currentJobs)
    {
        if (_premium.IsPremium || _premium.IsSubscriber)
            return true;

        return currentJobs < 1;
    }
    #endregion

    #region OB Templates
    /// <summary>
    /// Gratisanvändare får bara använda OB kväll/natt/helg.
    /// Premium och prenumeranter får alla OB-mallar.
    /// </summary>
    public bool CanUseAdvancedOB()
    {
        return _premium.IsPremium || _premium.IsSubscriber;
    }
    #endregion

    #region Jour
    /// <summary>
    /// Jour är endast tillgängligt för Premium och prenumeranter.
    /// </summary>
    public bool CanUseJour()
    {
        return _premium.IsPremium || _premium.IsSubscriber;
    }
    #endregion

    #region Export
    /// <summary>
    /// Export till PDF/Excel är endast för Premium och prenumeranter.
    /// </summary>
    public bool CanExport()
    {
        return _premium.IsPremium || _premium.IsSubscriber;
    }
    #endregion

    #region Subscription-only Features
    /// <summary>
    /// Automatiska backup är endast för prenumeranter.
    /// </summary>
    public bool CanUseBackup()
    {
        return _premium.IsSubscriber;
    }

    /// <summary>
    /// Extra teman är endast för prenumeranter.
    /// </summary>
    public bool CanUseExtraThemes()
    {
        return _premium.IsSubscriber;
    }

    #endregion
}
