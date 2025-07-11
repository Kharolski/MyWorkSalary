using MyWorkSalary.Models.Specialized;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IVacationLeaveRepository
    {
        // Grundläggande CRUD
        Task<VacationLeave> GetByIdAsync(int id);
        Task<int> InsertAsync(VacationLeave vacationLeave);
        Task<int> UpdateAsync(VacationLeave vacationLeave);
        Task<int> DeleteAsync(int id);

        // Semester-specifika queries 
        Task<List<VacationLeave>> GetByJobProfileAsync(int jobProfileId);
        Task<List<VacationLeave>> GetByJobProfileAndYearAsync(int jobProfileId, int year);
        Task<decimal> GetTotalVacationDaysUsedAsync(int jobProfileId, int year);
    }
}
