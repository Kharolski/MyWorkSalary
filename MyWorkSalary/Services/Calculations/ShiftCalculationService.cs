using MyWorkSalary.Models;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.Services.Calculations
{
    public class ShiftCalculationService : IShiftCalculationService
    {
        #region Constructor
        public ShiftCalculationService()
        {
        }
        #endregion

        #region Main Calculation Methods
        public (decimal Hours, decimal Pay) CalculateShiftHoursAndPay(
            DateTime selectedDate,
            TimeSpan startTime,
            TimeSpan endTime,
            ShiftType shiftType,
            int numberOfDays,
            JobProfile jobProfile)
        {
            try
            {
                // Specialhantering för semester/sjuk
                if (shiftType == ShiftType.Vacation || shiftType == ShiftType.SickLeave)
                {
                    var leavePay = shiftType == ShiftType.Vacation
                        ? CalculateVacationPay(numberOfDays, jobProfile)
                        : CalculateSickPay(numberOfDays, jobProfile);

                    return (Hours: 0, Pay: leavePay);
                }

                // Vanliga pass - beräkna timmar
                var startDateTime = selectedDate.Add(startTime);
                var endDateTime = selectedDate.Add(endTime);

                // Hantera pass över midnatt
                if (endTime < startTime)
                {
                    endDateTime = endDateTime.AddDays(1);
                }

                var duration = endDateTime - startDateTime;
                var hours = (decimal)duration.TotalHours;

                // Beräkna lön för vanliga pass
                var regularPay = jobProfile?.HourlyRate > 0
                    ? hours * jobProfile.HourlyRate.Value
                    : 0;

                return (Hours: hours, Pay: regularPay);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fel i CalculateShiftHoursAndPay: {ex.Message}");
                return (Hours: 0, Pay: 0);
            }
        }
        #endregion

        #region Leave Calculations
        public decimal CalculateVacationPay(int days, JobProfile jobProfile)
        {
            if (jobProfile?.HourlyRate == null || jobProfile.HourlyRate <= 0)
                return 0;

            // Semester = full lön, 8h/dag standard
            return days * 8 * jobProfile.HourlyRate.Value;
        }

        public decimal CalculateSickPay(int days, JobProfile jobProfile)
        {
            if (jobProfile?.HourlyRate == null || jobProfile.HourlyRate <= 0)
                return 0;

            if (days == 1)
            {
                return 0; // Karensdag
            }

            // Dag 1 = karens (0 kr), dag 2-7 = 80% från arbetsgivare
            var paidDays = Math.Min(days - 1, 6); // Max 6 dagar från arbetsgivare
            return paidDays * 8 * jobProfile.HourlyRate.Value * 0.8m;
        }
        #endregion

        #region Validation
        public bool ValidateHours(decimal hours)
        {
            return hours > 0 && hours <= 24;
        }
        #endregion

        #region Future Calculations
        // TODO: Lägg till OB-beräkningar här senare
        // TODO: Lägg till övertidsberäkningar här senare
        #endregion
    }
}
