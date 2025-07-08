using MyWorkSalary.Models.Core;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IFlexTimeRepository
    {
        // Basic CRUD
        FlexTimeBalance GetFlexTimeBalance(int jobProfileId, int year, int month);
        List<FlexTimeBalance> GetFlexTimeHistory(int jobProfileId);
        int SaveFlexTimeBalance(FlexTimeBalance flexBalance);
        void UpdateFlexTimeBalance(FlexTimeBalance flexBalance);
        int DeleteFlexTimeBalance(int id);

        // Business queries
        decimal GetCurrentFlexBalance(int jobProfileId);
        decimal GetPreviousFlexBalance(int jobProfileId, int year, int month);

        // Complex calculations
        void RecalculateRunningBalances(int jobProfileId, int fromYear, int fromMonth);

        // Validation & utilities
        bool HasFlexDataForMonth(int jobProfileId, int year, int month);
        List<FlexTimeBalance> GetFlexBalancesFromDate(int jobProfileId, int year, int month);
    }
}
