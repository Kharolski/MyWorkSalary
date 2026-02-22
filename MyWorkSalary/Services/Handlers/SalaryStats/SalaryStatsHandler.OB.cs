using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Reports;
using MyWorkSalary.Models.Specialized;

namespace MyWorkSalary.Services.Handlers
{
    public partial class SalaryStatsHandler
    {
        #region OB

        private void CalculateOBFromEvents(int jobId, DateTime payMonth, IEnumerable<WorkShift> shifts, SalaryStats stats)
        {
            var payYear = payMonth.Year;
            var payMonthNumber = payMonth.Month;

            var events = _obEventRepository.GetForPayPeriod(jobId, payYear, payMonthNumber)
                ?? new List<OBEvent>();

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

                var obByShift = events
                    .GroupBy(e => e.WorkShiftId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Hours));

                foreach (var shift in shifts)
                {
                    if (shift.ShiftType == ShiftType.OnCall)
                    {
                        shift.OBHours = 0;
                        continue;
                    }

                    shift.OBHours = obByShift.TryGetValue(shift.Id, out var h) ? h : 0;
                }

                return;
            }

            // ===== FALLBACK =====
            stats.UsedObFallback = true;
            stats.ObInfoNote = LocalizationHelper.Translate("ObInfo_FallbackUsed");

            var obRates = _salaryRepository
                .GetObShiftsForPeriod(jobId, DateTime.MinValue, DateTime.MaxValue)
                ?? Enumerable.Empty<OBRate>();

            stats.HasObRulesConfigured = obRates.Any(r => r.IsActive);

            if (!stats.HasObRulesConfigured)
            {
                stats.UsedObFallback = false;
                stats.ObInfoNote = LocalizationHelper.Translate("ObInfo_NoRulesConfigured");
                return;
            }

            foreach (var shift in shifts)
            {
                if (shift.ShiftType == ShiftType.OnCall)
                    continue;

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
                        IsDayMatch(rate, GetEffectiveDayForRate(rate, dayOfWeek, timeOfDay),
                            shift.IsHoliday, shift.IsBigHoliday) &&
                        IsTimeInRange(rate.StartTime, rate.EndTime, timeOfDay))
                    .OrderByDescending(r => r.Priority)
                    .FirstOrDefault();

                if (matchingRate != null)
                    result[matchingRate.Category] += 1m / 60m;

                current = current.AddMinutes(1);
            }

            return result
                .Select(kv => (kv.Key, Math.Round(kv.Value, 2)))
                .ToList();
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
            if (start == end)
                return true;

            if (start < end)
                return time >= start && time < end;

            return time >= start || time < end;
        }

        private OBDayType GetObDayType(DateTime date, bool isHoliday, bool isBigHoliday)
        {
            if (isBigHoliday)
                return OBDayType.BigHoliday;

            if (isHoliday)
                return OBDayType.Holiday;

            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                return OBDayType.Weekend;

            return OBDayType.Weekday;
        }

        private static DayOfWeek GetEffectiveDayForRate(OBRate rate, DayOfWeek currentDay, TimeSpan time)
        {
            // Om regeln går över midnatt (t.ex 22:00–06:00)
            // och vi är i "efter-midnatt"-delen (t.ex 00:15),
            // då ska vi matcha mot föregående dag.
            if (rate.StartTime > rate.EndTime && time < rate.EndTime)
            {
                return currentDay == DayOfWeek.Sunday
                    ? DayOfWeek.Saturday
                    : currentDay - 1;
            }

            return currentDay;
        }

        #endregion
    }
}
