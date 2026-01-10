using MyWorkSalary.Models.Specialized;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IShiftTimeSettingsRepository
    {
        ShiftTimeSettings GetForJob(int jobId);      // Hämta befintliga eller default
        void Save(ShiftTimeSettings settings);       // Spara ändringar
    }
}
