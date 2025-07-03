using MyWorkSalary.Models;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IShiftCalculationService
    {
        (decimal Hours, decimal Pay) CalculateShiftHoursAndPay(
            DateTime selectedDate,
            TimeSpan startTime,
            TimeSpan endTime,
            ShiftType shiftType,
            int numberOfDays,
            JobProfile jobProfile);

        decimal CalculateVacationPay(int days, JobProfile jobProfile);
        decimal CalculateSickPay(int days, JobProfile jobProfile);
        bool ValidateHours(decimal hours);
    }
}