using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Reports;
using MyWorkSalary.Helpers.Localization; // här ligger SalaryStats

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

            var totalHours = shifts.Sum(s => ShiftHoursSafe(s, periodStart, periodEnd));
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

            // Arbetade timmar (klippt inom vald period)
            var hoursStart = profile.EmploymentType == EmploymentType.Permanent ? payStart : workStart;
            var hoursEnd = profile.EmploymentType == EmploymentType.Permanent ? payEnd : workEnd;

            stats.ExtraPay = 0;
            // Extra pass: betalas i payMonth men avser workMonth
            stats.ExtraShiftDetails.Clear();

            if (profile.ExtraShiftEnabled)
            {
                var extraShifts = (_salaryRepository.GetShiftsForPeriod(jobId, workStart, workEnd) ?? Enumerable.Empty<WorkShift>())
                .Where(s => s.ShiftType == ShiftType.Regular)
                .Where(s => s.IsExtraShift)
                .Where(s => s.ExtraShiftPay > 0)
                .OrderBy(s => s.ShiftDate)
                .ToList();

                foreach (var s in extraShifts)
                {
                    stats.ExtraShiftDetails.Add(new ExtraShiftDetail
                    {
                        Date = s.ShiftDate,
                        Hours = s.TotalHours,
                        ExtraPay = s.ExtraShiftPay
                    });
                }

                stats.ExtraPay = Math.Round(extraShifts.Sum(s => s.ExtraShiftPay), 2);
            }

            stats.TotalHours = shifts.Sum(s => ShiftHoursSafe(s, hoursStart, hoursEnd));

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

        private decimal ShiftHoursSafe(WorkShift s, DateTime periodStart, DateTime periodEnd)
        {
            if (!s.StartTime.HasValue || !s.EndTime.HasValue)
                return 0m;

            var shiftStart = s.StartTime.Value;
            var shiftEnd = s.EndTime.Value;

            // säkerhet
            if (shiftEnd <= shiftStart)
                return 0m;

            // Klipp segmentet till perioden
            var from = shiftStart < periodStart ? periodStart : shiftStart;
            var to = shiftEnd > periodEnd ? periodEnd : shiftEnd;

            if (to <= from)
                return 0m;

            var overlapMinutes = (decimal)(to - from).TotalMinutes;
            if (overlapMinutes <= 0)
                return 0m;

            // Dra rast proportionellt utifrån hur stor del av passet som ligger i perioden
            var totalShiftMinutes = (decimal)(shiftEnd - shiftStart).TotalMinutes;
            var breakMinutes = (decimal)Math.Max(0, s.BreakMinutes);

            if (breakMinutes > 0 && totalShiftMinutes > 0)
            {
                var fraction = overlapMinutes / totalShiftMinutes;
                var breakToSubtract = breakMinutes * fraction;
                overlapMinutes = Math.Max(0, overlapMinutes - breakToSubtract);
            }

            return Math.Round(overlapMinutes / 60m, 2);
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
                stats.HasObRulesConfigured = true;
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
                        DayType = ev.DayType,
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
            stats.ObInfoNote = LocalizationHelper.Translate("ObInfo_FallbackUsed");

            // För fallback behöver vi OB-rates för jobbet
            // Om du inte har en repository här, kan du använda _salaryRepository.GetObShiftsForPeriod(jobId, ...) om den finns kvar.
            // (Din SalaryRepository returnerar alla OBRates ändå.)
            var obRates = _salaryRepository.GetObShiftsForPeriod(jobId, DateTime.MinValue, DateTime.MaxValue) ?? Enumerable.Empty<OBRate>();

            stats.HasObRulesConfigured = obRates.Any(r => r.IsActive);
            if (!stats.HasObRulesConfigured)
            {
                stats.UsedObFallback = false; // fallback gick inte att använda
                stats.ObInfoNote = LocalizationHelper.Translate("ObInfo_NoRulesConfigured");
                return;
            }

            foreach (var shift in shifts)
            {
                if (!shift.StartTime.HasValue || !shift.EndTime.HasValue)
                    continue;

                var isWeekend = shift.ShiftDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

                var dayType =
                    shift.IsBigHoliday ? OBDayType.BigHoliday :
                    shift.IsHoliday ? OBDayType.Holiday :
                    isWeekend ? OBDayType.Weekend :
                    OBDayType.Weekday;

                var obByCategory = CalculateObHoursByCategory(shift, obRates);

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
                        DayType = dayType,
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

                var matchingRate = obRates
                    .Where(rate =>
                        rate.IsActive &&
                        IsDayMatch(rate, dayOfWeek, shift.IsHoliday, shift.IsBigHoliday) &&
                        IsTimeInRange(rate.StartTime, rate.EndTime, timeOfDay))
                    .OrderByDescending(r => r.Priority)
                    .FirstOrDefault();

                if (matchingRate != null)
                    result[matchingRate.Category] += 1m / 60m;

                current = current.AddMinutes(1);
            }

            return result.Select(kv => (kv.Key, Math.Round(kv.Value, 2))).ToList();
        }

        private bool IsDayMatch(OBRate rate, DayOfWeek dayOfWeek, bool isHoliday, bool isBigHoliday)
        {
            if (isBigHoliday && rate.BigHolidays)
                return true;
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

        private static bool IsTimeInRange(TimeSpan start, TimeSpan end, TimeSpan time)
        {
            // SPECIAL: 00:00–00:00 (eller samma tid) = gäller hela dygnet
            if (start == end)
                return true;

            if (start < end)
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
