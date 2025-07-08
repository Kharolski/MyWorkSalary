using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.Services.Builders
{
    public class ShiftBuilderService : IShiftBuilderService
    {
        #region Leave Shifts (Semester/Sjuk)
        public WorkShift BuildLeaveShift(
            JobProfile jobProfile,
            DateTime selectedDate,
            ShiftType shiftType,
            int numberOfDays,
            decimal calculatedPay,
            string notes)
        {
            return new WorkShift
            {
                JobProfileId = jobProfile.Id,
                ShiftDate = selectedDate,
                StartTime = null,  // Inga tider för Semester/Sjuk
                EndTime = null,    // Inga tider för Semester/Sjuk
                ShiftType = shiftType,
                NumberOfDays = numberOfDays,
                TotalHours = 0,    // 0 timmar för Semester/Sjuk
                RegularHours = 0,
                OBHours = 0,
                RegularPay = calculatedPay,
                OBPay = 0,
                TotalPay = calculatedPay,
                Notes = notes,
                CreatedDate = DateTime.Now,
                IsConfirmed = false,
                // Sjukskrivning specifikt
                IsKarensDay = shiftType == ShiftType.SickLeave && numberOfDays >= 1,
                SickPayPercentage = shiftType == ShiftType.SickLeave ? 0.8m : null
            };
        }
        #endregion

        #region Regular Shifts
        public WorkShift BuildRegularShift(
            JobProfile jobProfile,
            DateTime selectedDate,
            TimeSpan startTime,
            TimeSpan endTime,
            ShiftType shiftType,
            decimal calculatedHours,
            decimal calculatedPay,
            string notes)
        {
            var startDateTime = selectedDate.Add(startTime);
            var endDateTime = selectedDate.Add(endTime);

            // Hantera pass över midnatt
            if (endTime < startTime)
            {
                endDateTime = endDateTime.AddDays(1);
            }

            return new WorkShift
            {
                JobProfileId = jobProfile.Id,
                ShiftDate = selectedDate,
                StartTime = startDateTime,
                EndTime = endDateTime,
                ShiftType = shiftType,
                NumberOfDays = null,
                TotalHours = calculatedHours,
                RegularHours = calculatedHours,
                OBHours = 0,
                RegularPay = calculatedPay,
                OBPay = 0,
                TotalPay = calculatedPay,
                Notes = notes,
                CreatedDate = DateTime.Now,
                IsConfirmed = false
            };
        }
        #endregion
    }
}
