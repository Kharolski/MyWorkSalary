using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IConflictHandlerService
    {
        Task HandleSickLeaveConflict(string message, WorkShift sickShift);
        Task HandleWorkShiftSickConflict(string message, WorkShift workShift, JobProfile activeJob);
        Task HandleWorkShiftConflict(string message, WorkShift leaveShift);
        Task HandlePeriodMerging(string message, WorkShift newShift);
    }
}
