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
        private readonly IOBEventRepository _obEventRepository;

        #endregion

        #region Constructor

        public SalaryStatsHandler(ISalaryRepository salaryRepository, IOBEventRepository obEventRepository)
        {
            _salaryRepository = salaryRepository ?? throw new ArgumentNullException(nameof(salaryRepository));
            _obEventRepository = obEventRepository ?? throw new ArgumentNullException(nameof(obEventRepository));
        }

        #endregion

        #region Monthly Salary
        /// <summary>
        /// Hämtar månadslön för en specifik månad och jobb.
        /// </summary>
        public decimal GetMonthlySalary(int jobId, DateTime periodStart, DateTime periodEnd)
        {
            var profile = _salaryRepository.GetJobProfile(jobId);
            if (profile == null)
                return 0;

            if (profile.EmploymentType == EmploymentType.Permanent)
                return profile.MonthlySalary ?? 0;

            var shifts = _salaryRepository.GetShiftsForPeriod(jobId, periodStart, periodEnd) ?? Enumerable.Empty<WorkShift>();
            var totalHours = shifts.Sum(ShiftHoursSafe);
            return totalHours * (profile.HourlyRate ?? 0);
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

            // month = utbetalningsmånad
            var payMonth = new DateTime(month.Year, month.Month, 1);
            var workMonth = payMonth.AddMonths(-1);

            var (payStart, payEnd) = GetCalendarMonth(payMonth);
            var (workStart, workEnd) = GetCalendarMonth(workMonth);

            // Hämta pass beroende på anställningstyp
            IEnumerable<WorkShift> shifts =
                profile.EmploymentType == EmploymentType.Permanent
                    ? (_salaryRepository.GetShiftsForPeriod(jobId, payStart, payEnd) ?? Enumerable.Empty<WorkShift>())
                    : (_salaryRepository.GetShiftsForPeriod(jobId, workStart, workEnd) ?? Enumerable.Empty<WorkShift>());

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

            // Arbetade timmar (den period vi valt ovan)
            stats.TotalHours = shifts.Sum(ShiftHoursSafe);

            // Grundlön:
            // - Fast: payMonth
            // - Tim: workMonth (det som betalas i payMonth)
            stats.BaseSalary =
                profile.EmploymentType == EmploymentType.Permanent
                    ? (profile.MonthlySalary ?? 0)
                    : GetMonthlySalary(jobId, workStart, workEnd);

            // OB: alltid via OBEvent för utbetalningsmånad (PayYear/PayMonth == payMonth)
            CalculateOBFromEvents(jobId, payMonth, shifts, stats);

            // Semesterersättning (timanställd)
            if (profile.EmploymentType != EmploymentType.Permanent)
            {
                // Exempel: 12% semesterersättning (vanligt i Sverige) kan ändras till användarens val senare 
                const decimal vacationRate = 0.12m;
                stats.VacationPay = Math.Round(stats.BaseSalary * vacationRate, 2);
            }
            else
            {
                stats.VacationPay = 0;
            }

            // Skatt 
            CalculateTax(profile, stats);

            return stats;
        }

        private (DateTime start, DateTime end) GetCalendarMonth(DateTime month)
        {
            var start = new DateTime(month.Year, month.Month, 1);
            var end = start.AddMonths(1);
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
        private void CalculateOBFromEvents(int jobId, DateTime payMonth, IEnumerable<WorkShift> shifts, SalaryStats stats)
        {
            var payYear = payMonth.Year;
            var payMonthNumber = payMonth.Month;

            var events = _obEventRepository.GetForPayPeriod(jobId, payYear, payMonthNumber) ?? new List<OBEvent>();

            stats.TotalObHours = 0;
            stats.ObPay = 0;
            stats.ObDetails.Clear();

            if (events.Any())
            {
                stats.UsedObFallback = false;
                stats.ObInfoNote = null;

                foreach (var ev in events)
                {
                    stats.TotalObHours += ev.Hours;
                    stats.ObPay += ev.TotalAmount;

                    stats.ObDetails.Add(new ObDetails
                    {
                        Date = ev.WorkDate,
                        Hours = ev.Hours,
                        RatePerHour = ev.RatePerHour,
                        Category = ev.OBType,
                        Pay = ev.TotalAmount
                    });
                }

                // sätt OBHours per shift
                var obByShift = events
                    .GroupBy(e => e.WorkShiftId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Hours));

                foreach (var shift in shifts)
                    shift.OBHours = obByShift.TryGetValue(shift.Id, out var h) ? h : 0;

                return;
            }

            // ===== FALLBACK: räkna "live" från pass + OBRates =====
            stats.UsedObFallback = true;
            stats.ObInfoNote = "OB beräknas från nuvarande regler (OB-händelser saknas för perioden).";

            // För fallback behöver vi OB-rates för jobbet
            // Om du inte har en repository här, kan du använda _salaryRepository.GetObShiftsForPeriod(jobId, ...) om den finns kvar.
            // (Din SalaryRepository returnerar alla OBRates ändå.)
            var obRates = _salaryRepository.GetObShiftsForPeriod(jobId, DateTime.MinValue, DateTime.MaxValue) ?? Enumerable.Empty<OBRate>();

            foreach (var shift in shifts)
            {
                if (!shift.StartTime.HasValue || !shift.EndTime.HasValue)
                    continue;

                var obByCategory = CalculateObHoursByCategory(shift, obRates);

                // Respektera ShiftTimeSettings-snapshots
                obByCategory = obByCategory
                    .Where(x =>
                        (x.Category != OBCategory.Evening || shift.EveningActiveAtThatTime) &&
                        (x.Category != OBCategory.Night || shift.NightActiveAtThatTime))
                    .ToList();

                foreach (var (category, hours) in obByCategory)
                {
                    if (hours <= 0)
                        continue;

                    var rate = obRates.FirstOrDefault(r => r.IsActive && r.Category == category);
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

                shift.OBHours = obByCategory.Sum(x => x.Hours);
            }
        }

        private List<(OBCategory Category, decimal Hours)> CalculateObHoursByCategory(WorkShift shift, IEnumerable<OBRate> obRates)
        {
            var result = new Dictionary<OBCategory, decimal>();

            foreach (OBCategory cat in Enum.GetValues(typeof(OBCategory)))
                result[cat] = 0m;

            var start = shift.StartTime!.Value;
            var end = shift.EndTime!.Value;

            if (end < start)
                end = end.AddDays(1);

            var current = start;

            while (current < end)
            {
                var timeOfDay = current.TimeOfDay;
                var dayOfWeek = current.DayOfWeek;

                var matchingRate = obRates.FirstOrDefault(rate =>
                    rate.IsActive &&
                    IsDayMatch(rate, dayOfWeek, shift.IsHoliday) &&
                    IsTimeInRange(rate.StartTime, rate.EndTime, timeOfDay));

                if (matchingRate != null)
                    result[matchingRate.Category] += 1m / 60m;

                current = current.AddMinutes(1);
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
                return time >= start && time < end;

            // över midnatt (t.ex 22:00–06:00)
            return time >= start || time < end;
        }
        #endregion

        #region Tax / Net Salary

        private void CalculateTax(JobProfile profile, SalaryStats stats)
        {
            if (profile == null)
                return;

            // Hämta effektiv skattesats (0.33 t.ex.)
            var taxRate = profile.EffectiveTaxRate;

            // Säkerhetskontroll (om användaren råkat skriva 33 istället för 0.33)
            if (taxRate > 1m)
                taxRate /= 100m;

            taxRate = Math.Clamp(taxRate, 0m, 1m);

            stats.TaxRate = taxRate;
            stats.TaxAmount = Math.Round(stats.GrossSalary * taxRate, 2);
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
