using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Specialized;
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
            JobProfile jobProfile,
            int breakMinutes = 0)
        {
            try
            {
                // Specialhantering för olika typer
                return shiftType switch
                {
                    ShiftType.Vacation => (Hours: 0, Pay: CalculateVacationPay(numberOfDays, jobProfile)),
                    ShiftType.VAB => CalculateVABShift(selectedDate, jobProfile), 
                    ShiftType.Regular or ShiftType.OnCall => CalculateRegularShift(selectedDate, startTime, endTime, jobProfile, breakMinutes),
                    _ => (Hours: 0, Pay: 0)
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fel i CalculateShiftHoursAndPay: {ex.Message}");
                return (Hours: 0, Pay: 0);
            }
        }

        /// <summary>
        /// Beräknar vanliga arbetspass med rast
        /// </summary>
        private (decimal Hours, decimal Pay) CalculateRegularShift(
            DateTime selectedDate,
            TimeSpan startTime,
            TimeSpan endTime,
            JobProfile jobProfile,
            int breakMinutes)
        {
            var startDateTime = selectedDate.Add(startTime);
            var endDateTime = selectedDate.Add(endTime);

            // Hantera pass över midnatt
            if (endTime < startTime)
            {
                endDateTime = endDateTime.AddDays(1);
            }

            var duration = endDateTime - startDateTime;
            var totalMinutes = (decimal)duration.TotalMinutes;

            // Dra av rast från arbetstid
            var workingMinutes = totalMinutes - breakMinutes;
            var hours = workingMinutes / 60;

            // Säkerställ att timmar inte blir negativa
            hours = Math.Max(0, hours);

            // Beräkna lön
            var regularPay = jobProfile?.HourlyRate > 0
                ? hours * jobProfile.HourlyRate.Value
                : 0;

            return (Hours: hours, Pay: regularPay);
        }

        /// <summary>
        /// Beräknar VAB-pass (använder VAB-logiken)
        /// </summary>
        private (decimal Hours, decimal Pay) CalculateVABShift(DateTime selectedDate, JobProfile jobProfile)
        {
            // VAB = inga arbetstimmar, men kan ha ersättning från Försäkringskassan
            var vabResult = CalculateVABDeduction(selectedDate, jobProfile);

            // Returnera 0 timmar och VAB-ersättning från företaget 0
            return (Hours: 0, Pay: 0);
        }

        public ShiftCalculationResult CalculateOBHours(DateTime workDate, TimeSpan startTime, TimeSpan endTime, ShiftTimeSettings settings)
        {
            var result = new ShiftCalculationResult
            {
                EveningStart = settings.EveningStart,
                NightStart = settings.NightStart,
                EveningActive = settings.EveningActive,
                NightActive = settings.NightActive
            };
            // Calculate total shift duration
            var totalMinutes = (endTime - startTime).TotalMinutes;
            if (totalMinutes <= 0)
                return result;
            // Check if it's weekend
            bool isWeekend = workDate.DayOfWeek == DayOfWeek.Saturday ||
                            workDate.DayOfWeek == DayOfWeek.Sunday;
            // Use weekend settings if it's weekend
            if (isWeekend && settings.WeekendEveningActive)
            {
                result.EveningStart = settings.WeekendEveningStart;
                result.EveningActive = true;
            }
            if (isWeekend && settings.WeekendNightActive)
            {
                result.NightStart = settings.WeekendNightStart;
                result.NightActive = true;
            }
            // Calculate OB hours
            if (result.EveningActive && startTime < result.EveningStart && endTime > result.EveningStart)
            {
                var eveningEnd = result.NightActive ? result.NightStart : endTime;
                result.EveningHours = (decimal)(eveningEnd - result.EveningStart).TotalHours;
            }
            if (result.NightActive && startTime < result.NightStart && endTime > result.NightStart)
            {
                result.NightHours = (decimal)(endTime - result.NightStart).TotalHours;
            }
            return result;
        }
        #endregion

        #region CalculateRegularShift
        public ShiftCalculationResult CalculateRegularShiftDetailed(
                DateTime selectedDate,
                TimeSpan startTime,
                TimeSpan endTime,
                JobProfile jobProfile,
                ShiftTimeSettings shiftSettings,
                decimal eveningOBRate,
                decimal nightOBRate,
                int breakMinutes)
        {
            var result = new ShiftCalculationResult();

            // Start / slut (hantera midnatt)
            var startDateTime = selectedDate.Date.Add(startTime);
            var endDateTime = selectedDate.Date.Add(endTime);

            if (endTime < startTime)
                endDateTime = endDateTime.AddDays(1);

            var totalMinutes = (decimal)(endDateTime - startDateTime).TotalMinutes;

            // Break minutes are deducted from total time, not from specific OB periods
            var workingMinutes = Math.Max(0, totalMinutes - breakMinutes);
            result.TotalHours = workingMinutes / 60;

            // Snapshot (historik & icons)
            result.EveningStart = shiftSettings.EveningStart;
            result.NightStart = shiftSettings.NightStart;
            result.EveningActive = shiftSettings.EveningActive;
            result.NightActive = shiftSettings.NightActive;

            // Bygg gränser
            var eveningStart = selectedDate.Date.Add(shiftSettings.EveningStart);
            var nightStart = selectedDate.Date.Add(shiftSettings.NightStart);

            if (shiftSettings.NightStart < shiftSettings.EveningStart)
                nightStart = nightStart.AddDays(1);

            // Kväll
            if (shiftSettings.EveningActive && endDateTime > eveningStart)
            {
                var eveningFrom = startDateTime > eveningStart ? startDateTime : eveningStart;
                var eveningTo = endDateTime < nightStart ? endDateTime : nightStart;

                if (eveningTo > eveningFrom)
                    result.EveningHours = (decimal)(eveningTo - eveningFrom).TotalHours;
            }

            // Natt
            if (shiftSettings.NightActive && endDateTime > nightStart)
            {
                var nightFrom = startDateTime > nightStart ? startDateTime : nightStart;

                if (endDateTime > nightFrom)
                    result.NightHours = (decimal)(endDateTime - nightFrom).TotalHours;
            }

            // Vanliga timmar
            result.RegularHours = Math.Max(
                0,
                result.TotalHours - result.EveningHours - result.NightHours
            );

            // Löner
            var hourlyRate = jobProfile.HourlyRate ?? 0;

            result.RegularPay = result.RegularHours * hourlyRate;

            result.EveningOBRate = eveningOBRate;
            result.NightOBRate = nightOBRate;

            result.EveningOBPay = result.EveningHours * eveningOBRate;
            result.NightOBPay = result.NightHours * nightOBRate;

            return result;
        }
        #endregion

        #region Semester Calculations
        public decimal CalculateVacationPay(int days, JobProfile jobProfile)
        {
            if (jobProfile?.HourlyRate == null || jobProfile.HourlyRate <= 0)
                return 0;

            // Semester = full lön, 8h/dag standard
            return days * 8 * jobProfile.HourlyRate.Value;
        }
        #endregion

        #region VAB Calculations
        /// <summary>
        /// Beräknar VAB-avdrag för månadslön
        /// </summary>
        /// <param name="date">VAB-datum</param>
        /// <param name="jobProfile">Jobbprofil</param>
        /// <returns>VAB avdragsinformation</returns>
        public VABDeductionResult CalculateVABDeduction(DateTime date, JobProfile jobProfile)
        {
            // Timanställd - inget avdrag (FK betalar)
            if (jobProfile.EmploymentType == EmploymentType.Temporary)
            {
                return new VABDeductionResult
                {
                    HasDeduction = false,
                    DeductionAmount = 0,
                    ExpectedHours = 0,
                    Description = LocalizationHelper.Translate("VAB_Description_Hourly")
                };
            }

            // Månadslön - beräkna avdrag
            if (jobProfile.MonthlySalary.HasValue && jobProfile.MonthlySalary > 0)
            {
                var expectedHours = 8.0m; // Standard arbetsdag
                var dailyRate = jobProfile.MonthlySalary.Value / 21; // Genomsnittliga arbetsdagar per månad

                return new VABDeductionResult
                {
                    HasDeduction = true,
                    DeductionAmount = dailyRate,
                    ExpectedHours = expectedHours,
                    Description = string.Format(
                        LocalizationHelper.Translate("VAB_Description_WithDeduction"),
                        expectedHours,
                        dailyRate)
                };
            }

            // Timlön med fast anställning
            if (jobProfile.HourlyRate.HasValue && jobProfile.HourlyRate > 0)
            {
                var expectedHours = 8.0m;
                var dailyRate = expectedHours * jobProfile.HourlyRate.Value;

                return new VABDeductionResult
                {
                    HasDeduction = true,
                    DeductionAmount = dailyRate,
                    ExpectedHours = expectedHours,
                    Description = string.Format(
                        LocalizationHelper.Translate("VAB_Description_WithDeduction"),
                        expectedHours,
                        dailyRate)
                };
            }

            // Fallback
            return new VABDeductionResult
            {
                HasDeduction = false,
                DeductionAmount = 0,
                ExpectedHours = 0,
                Description = LocalizationHelper.Translate("VAB_Description_NoInfo")
            };
        }
        #endregion

        #region Sick Calculations
        public decimal CalculateHourlyRateFromMonthlySalary(JobProfile jobProfile)
        {
            if (jobProfile.MonthlySalary == null || jobProfile.MonthlySalary <= 0)
                throw new ArgumentException(LocalizationHelper.Translate("Error_SalaryRequired"));

            // Standardberäkning: (Månadslön × 12) ÷ (52 veckor × genomsnittliga timmar/vecka)
            // Antar 40 timmar/vecka för fast anställd
            var yearlyHours = 52 * 40; // 2080 timmar per år
            return (jobProfile.MonthlySalary.Value * 12) / yearlyHours;
        }
        #endregion

        #region Break Calculations
        /// <summary>
        /// Föreslår rast baserat på arbetstid (endast som hjälp - användaren bestämmer)
        /// </summary>
        /// <param name="workingHours">Arbetstimmar</param>
        /// <returns>Föreslagen rast i minuter (användaren kan ignorera)</returns>
        public int SuggestBreakMinutes(decimal workingHours)
        {
            // Bara förslag - användaren kan skriva över
            return workingHours switch
            {
                >= 8 => 45,  // 8+ timmar = 45 min (lunch + fika)
                >= 6 => 30,  // 6-8 timmar = 30 min lunch
                >= 4 => 15,  // 4-6 timmar = 15 min fika
                _ => 0       // Under 4 timmar = ingen rast
            };
        }

        /// <summary>
        /// Hämtar rast-förslag som text för UI
        /// </summary>
        /// <param name="workingHours">Arbetstimmar</param>
        /// <returns>Förslag-text för användaren</returns>
        public string GetBreakSuggestionText(decimal workingHours)
        {
            var suggestedMinutes = SuggestBreakMinutes(workingHours);

            if (suggestedMinutes == 0)
                return LocalizationHelper.Translate("Break_NoBreakSuggested");

            return string.Format(
                LocalizationHelper.Translate("Break_SuggestionTemplate"),
                suggestedMinutes);
        }

        /// <summary>
        /// Validerar att rast-minuter är rimliga
        /// </summary>
        /// <param name="breakMinutes">Rast i minuter</param>
        /// <param name="totalHours">Total arbetstid inkl. rast</param>
        /// <returns>True om giltig</returns>
        public bool ValidateBreakMinutes(int breakMinutes, decimal totalHours)
        {
            // Negativ rast = fel
            if (breakMinutes < 0)
                return false;

            // Rast längre än arbetstid = fel
            if (breakMinutes >= (totalHours * 60))
                return false;

            // Max 2 timmar rast per dag = rimligt
            if (breakMinutes > 120)
                return false;

            return true;
        }
        #endregion

        #region Validation
        public bool ValidateHours(decimal hours)
        {
            return hours > 0 && hours <= 24;
        }
        #endregion

    }

    #region Result Classes
    /// <summary>
    /// Resultat av VAB-avdragsberäkning
    /// </summary>
    public class VABDeductionResult
    {
        public bool HasDeduction { get; set; }
        public decimal DeductionAmount { get; set; }
        public decimal ExpectedHours { get; set; }
        public string Description { get; set; } = string.Empty;
    }
    #endregion
}
