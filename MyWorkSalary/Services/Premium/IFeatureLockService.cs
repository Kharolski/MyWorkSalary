namespace MyWorkSalary.Services.Premium;

public interface IFeatureLockService
{
    bool CanAddMoreJobs(int currentJobs);
    bool CanUseAdvancedOB();
    bool CanUseJour();
    bool CanExport();
    bool CanUseBackup();
    bool CanUseExtraThemes();
}
