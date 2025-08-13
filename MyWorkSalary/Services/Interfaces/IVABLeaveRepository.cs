using MyWorkSalary.Models.Specialized;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IVABLeaveRepository
    {
        // CRUD Operations
        Task<int> InsertAsync(VABLeave vabLeave);
        Task<int> DeleteAsync(int id);
        Task<VABLeave> GetByIdAsync(int id);

        // Query Operations
        Task<VABLeave> GetByWorkShiftIdAsync(int workShiftId);
        VABLeave GetByWorkShiftId(int workShiftId);
        Task<List<VABLeave>> GetByJobProfileIdAsync(int jobProfileId);
        Task<List<VABLeave>> GetByDateRangeAsync(int jobProfileId, DateTime startDate, DateTime endDate);
        Task<List<VABLeave>> GetAllAsync();

        // Business Logic Queries
        Task<decimal> GetTotalVABHoursAsync(int jobProfileId, DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalVABDeductionAsync(int jobProfileId, DateTime startDate, DateTime endDate);
        Task<int> GetVABDaysCountAsync(int jobProfileId, DateTime startDate, DateTime endDate);

        // Utility
        Task<bool> ExistsAsync(int workShiftId);
        Task<int> DeleteByJobProfileIdAsync(int jobProfileId);
    }
}
