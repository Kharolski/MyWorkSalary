using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IWorkShiftService
    {
        Task<(bool Success, string Message)> SaveWorkShiftWithValidation(WorkShift workShift);
    }
}
