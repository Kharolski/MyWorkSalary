using MyWorkSalary.Models.Specialized;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IOBRateRepository
    {
        // Basic CRUD
        List<OBRate> GetOBRates(int jobProfileId);
        OBRate GetOBRate(int id);
        int SaveOBRate(OBRate obRate);
        int DeleteOBRate(int id);

        // Business queries
        List<OBRate> GetActiveOBRates(int jobProfileId);
        OBRate GetOBRateForTime(int jobProfileId, TimeSpan time, DayOfWeek dayOfWeek);

        // Validation
        bool HasOverlappingRates(OBRate obRate);
        bool CanDeleteOBRate(int id);

        // Bulk operations
        void DeleteAllOBRates(int jobProfileId);
        void SaveMultipleOBRates(List<OBRate> obRates);

    }
}
