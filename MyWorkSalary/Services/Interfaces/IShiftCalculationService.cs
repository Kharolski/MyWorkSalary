using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Calculations;

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
            JobProfile jobProfile,
            int breakMinutes = 0);

        // Semester
        decimal CalculateVacationPay(int days, JobProfile jobProfile);

        // Vård av barn - VAB
        VABDeductionResult CalculateVABDeduction(DateTime date, JobProfile jobProfile);

        // Sjukskrivning
        public decimal CalculateHourlyRateFromMonthlySalary(JobProfile jobProfile);

        // Arbets timmar/Rast
        bool ValidateHours(decimal hours);
        int SuggestBreakMinutes(decimal workingHours);
        bool ValidateBreakMinutes(int breakMinutes, decimal totalHours);
        string GetBreakSuggestionText(decimal workingHours);
    }
}