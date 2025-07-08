using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IShiftBuilderService
    {
        WorkShift BuildLeaveShift(
            JobProfile jobProfile,
            DateTime selectedDate,
            ShiftType shiftType,
            int numberOfDays,
            decimal calculatedPay,
            string notes);

        WorkShift BuildRegularShift(
            JobProfile jobProfile,
            DateTime selectedDate,
            TimeSpan startTime,
            TimeSpan endTime,
            ShiftType shiftType,
            decimal calculatedHours,
            decimal calculatedPay,
            string notes);
    }
}
