using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Reports; // här ligger SalaryStats
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyWorkSalary.Services.Handlers
{
    /// <summary>
    /// Hanterar statistik och beräkningar för lönerapporter.
    /// </summary>
    public class SalaryStatsHandler
    {
        #region Fields

        private readonly ISalaryRepository _salaryRepository;

        #endregion

        #region Constructor

        public SalaryStatsHandler(ISalaryRepository salaryRepository)
        {
            _salaryRepository = salaryRepository ?? throw new ArgumentNullException(nameof(salaryRepository));
        }

        #endregion

        #region Monthly Salary
        /// <summary>
        /// Hämtar månadslön för en specifik månad och jobb.
        /// </summary>
        public decimal GetMonthlySalary(int jobId, DateTime month)
        {
            var profile = _salaryRepository.GetJobProfile(jobId);

            if (profile == null)
                return 0;

            if (profile.EmploymentType == EmploymentType.Permanent)
                return profile.MonthlySalary ?? 0;

            var (start, end) = GetCalendarMonth(month);

            var shifts = _salaryRepository.GetShiftsForPeriod(jobId, start, end);

            var totalHours = shifts
                .Where(s => s.StartTime.HasValue && s.EndTime.HasValue)
                .Sum(s => (s.EndTime.Value - s.StartTime.Value).TotalHours);

            return (decimal)totalHours * (profile.HourlyRate ?? 0);
        }
        #endregion

        #region Salary Stats
        /// <summary>
        /// Sammanställer enkel statistik för en månad (utan OB/VAB/jour än).
        /// </summary>
        public SalaryStats CalculateMonthlyStats(int jobId, DateTime month)
        {
            var stats = new SalaryStats();

            var profile = _salaryRepository.GetJobProfile(jobId);
            if (profile == null)
                return stats;

            var (start, end) = GetCalendarMonth(month);

            var shifts = _salaryRepository.GetShiftsForPeriod(jobId, start, end) ?? Enumerable.Empty<WorkShift>();
            var obRates = _salaryRepository.GetObShiftsForPeriod(jobId, start, end);

            // Nollställ innan beräkning
            stats.TotalHours = 0;
            stats.ExpectedHours = profile.ExpectedHoursPerMonth;
            stats.BaseSalary = 0;
            stats.TotalObHours = 0;
            stats.ObPay = 0;
            stats.ObDetails.Clear();
            stats.FlexBalance = 0;
            stats.SickDays = 0;
            stats.VacationDays = 0;
            stats.VabDays = 0;
            stats.OvertimePay = 0;
            stats.ExtraPay = 0;

            // Beräkna arbetade timmar
            stats.TotalHours = shifts.Sum(ShiftHoursSafe);

            // Grundlön
            stats.BaseSalary = GetMonthlySalary(jobId, month);

            // OB-timmar och OB-lön
            CalculateOB(jobId, shifts, stats, start, end);

            // Sätt OBHours per shift direkt från obByCategory istället för att summera från stats.ObDetails
            foreach (var shift in shifts)
            {
                if (!shift.StartTime.HasValue || !shift.EndTime.HasValue)
                {
                    shift.OBHours = 0;
                    continue;
                }

                var obByCategory = CalculateObHoursByCategory(shift, obRates);
                shift.OBHours = obByCategory.Sum(x => x.Hours);
            }

            return stats;
        }

        private (DateTime start, DateTime end) GetCalendarMonth(DateTime month)
        {
            var start = new DateTime(month.Year, month.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            return (start, end);
        }

        private decimal ShiftHoursSafe(WorkShift s)
        {
            if (s.TotalHours > 0)
                return s.TotalHours;

            if (s.StartTime.HasValue && s.EndTime.HasValue)
            {
                var hours = (decimal)(s.EndTime.Value - s.StartTime.Value).TotalHours;
                if (s.BreakMinutes > 0)
                    hours -= s.BreakMinutes / 60m;
                return Math.Max(0, hours);
            }

            return 0;
        }
        #endregion

        #region OB
        /// <summary>
        /// Beräknar OB-timmar och OB-lön för en lista skift och uppdaterar SalaryStats.
        /// </summary>
        private void CalculateOB(int jobId, IEnumerable<WorkShift> shifts, SalaryStats stats, DateTime start, DateTime end)
        {
            var obRates = _salaryRepository.GetObShiftsForPeriod(jobId, start, end);

            foreach (var shift in shifts)
            {
                if (!shift.StartTime.HasValue || !shift.EndTime.HasValue)
                    continue;

                var obByCategory = CalculateObHoursByCategory(shift, obRates);

                foreach (var (category, hours) in obByCategory)
                {
                    if (hours <= 0)
                        continue;

                    // Hitta rätt OB-rate för kategorin
                    var rate = obRates.FirstOrDefault(r => r.Category == category);
                    if (rate == null)
                        continue;

                    var pay = hours * rate.RatePerHour;
                    stats.TotalObHours += hours;
                    stats.ObPay += pay;
                    stats.ObDetails.Add(new ObDetails
                    {
                        Date = shift.ShiftDate,
                        Hours = hours,
                        RatePerHour = rate.RatePerHour,
                        Category = category,
                        Pay = pay
                    });
                }

                // Spara OB per shift
                shift.OBHours = obByCategory.Sum(x => x.Hours);
            }
        }

        private List<(OBCategory Category, decimal Hours)> CalculateObHoursByCategory(WorkShift shift, IEnumerable<OBRate> obRates)
        {
            var result = new Dictionary<OBCategory, decimal>();

            foreach (OBCategory cat in Enum.GetValues(typeof(OBCategory)))
                result[cat] = 0;

            var start = shift.StartTime.Value;
            var end = shift.EndTime.Value;

            if (end < start)
                end = end.AddDays(1);

            var current = start;

            while (current < end)
            {
                var next = current.AddMinutes(1);
                var timeOfDay = current.TimeOfDay;
                var dayOfWeek = current.DayOfWeek;

                // Hitta OB-rate som matchar denna tid och dag
                var matchingRate = obRates.FirstOrDefault(rate => 
                    rate.IsActive && 
                    IsDayMatch(rate, dayOfWeek, shift.IsHoliday) &&
                    IsTimeInRange(rate.StartTime, rate.EndTime, timeOfDay));

                if (matchingRate != null)
                {
                    result[matchingRate.Category] += 1m / 60m;
                }

                current = next;
            }

            return result.Select(kv => (kv.Key, Math.Round(kv.Value, 2))).ToList();
        }

        private bool IsDayMatch(OBRate rate, DayOfWeek dayOfWeek, bool isHoliday)
        {
            if (isHoliday && rate.Holidays)
                return true;

            return dayOfWeek switch
            {
                DayOfWeek.Monday => rate.Monday,
                DayOfWeek.Tuesday => rate.Tuesday,
                DayOfWeek.Wednesday => rate.Wednesday,
                DayOfWeek.Thursday => rate.Thursday,
                DayOfWeek.Friday => rate.Friday,
                DayOfWeek.Saturday => rate.Saturday,
                DayOfWeek.Sunday => rate.Sunday,
                _ => false
            };
        }

        private bool IsTimeInRange(TimeSpan start, TimeSpan end, TimeSpan time)
        {
            if (start <= end)
            {
                // Normal tid (t.ex. 18:00 - 22:00)
                return time >= start && time < end;
            }
            else
            {
                // Övergripande tid (t.ex. 22:00 - 06:00)
                return time >= start || time < end;
            }
        }
        #endregion

        #region Leave
        // Kommer senare: GetSickDays, GetVacationDays, GetVABDays etc.
        #endregion

        #region OnCall / Jour
        // Kommer senare: GetOnCallHours etc.
        #endregion
    }
}
