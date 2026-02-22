using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Reports;
using MyWorkSalary.Models.Specialized;

namespace MyWorkSalary.Services.Handlers
{
    public partial class SalaryStatsHandler
    {
        #region OnCall / Jour

        private void ApplyOnCall(
            int jobId,
            JobProfile profile,
            IEnumerable<WorkShift> onCallShifts,
            DateTime periodStart,
            DateTime periodEnd,
            SalaryStats stats)
        {
            if (profile == null || !profile.OnCallEnabled)
                return;

            if (onCallShifts == null)
                return;

            var obRates = _salaryRepository
                .GetObShiftsForPeriod(jobId, DateTime.MinValue, DateTime.MaxValue)?
                .Where(r => r.IsActive)
                .ToList() ?? new List<OBRate>();

            foreach (var ws in onCallShifts)
            {
                if (ws.ShiftType != ShiftType.OnCall)
                    continue;

                if (!ws.StartTime.HasValue || !ws.EndTime.HasValue)
                    continue;

                var ocs = _onCallRepository.GetByWorkShiftId(ws.Id);
                if (ocs == null)
                    continue;

                // ===== Standby =====
                var standbyHours = Math.Round(ocs.StandbyHours, 2);
                var standbyPay = Math.Round(
                    CalcStandbyPay(ocs.StandbyPayTypeSnapshot, ocs.StandbyPayAmountSnapshot, standbyHours),
                    2);

                stats.OnCallStandbyHours += standbyHours;
                stats.OnCallPay += standbyPay;

                // ===== Callouts =====
                var callouts = _onCallCalloutRepository.GetByOnCallShiftId(ocs.Id)
                    ?? new List<OnCallCallout>();

                var calloutDetails = new List<OnCallCalloutDetail>();

                decimal activeHoursInPeriod = 0m;
                decimal activeBasePay = 0m;

                var standbyStartDT = ws.StartTime.Value;
                var standbyEndDT = ws.EndTime.Value;
                if (standbyEndDT <= standbyStartDT)
                    standbyEndDT = standbyEndDT.AddDays(1);

                foreach (var c in callouts)
                {
                    var (cStartDT, cEndDT) = BuildCalloutDateTimes(
                        ws.ShiftDate,
                        ocs.StandbyStartTime,
                        standbyStartDT,
                        c.StartTime,
                        c.EndTime);

                    foreach (var (segFrom, segTo) in SplitByMidnight(cStartDT, cEndDT))
                    {
                        var segHours = OverlapHours(segFrom, segTo, periodStart, periodEnd);
                        if (segHours <= 0)
                            continue;

                        segHours = Math.Round(segHours, 2);

                        activeHoursInPeriod += segHours;

                        var segPay = Math.Round(segHours * ocs.ActiveHourlyRateSnapshot, 2);
                        activeBasePay += segPay;

                        calloutDetails.Add(new OnCallCalloutDetail
                        {
                            Date = segFrom.Date,
                            Start = segFrom.TimeOfDay,
                            End = segTo.TimeOfDay,
                            Hours = segHours,
                            Notes = c.Notes,
                            ActivePay = segPay
                        });

                        // OB på aktiv tid (valfritt)
                        if (obRates.Count > 0)
                        {
                            var segDate = segFrom.Date;

                            var dayType = GetObDayType(segDate, ws.IsHoliday, ws.IsBigHoliday);

                            var tempShift = new WorkShift
                            {
                                ShiftDate = segDate,
                                StartTime = segFrom,
                                EndTime = segTo,
                                IsHoliday = dayType == OBDayType.Holiday,
                                IsBigHoliday = dayType == OBDayType.BigHoliday
                            };

                            var obByCategory = CalculateObHoursByCategory(tempShift, obRates);

                            foreach (var (category, obHoursRaw) in obByCategory)
                            {
                                var obHours = Math.Round(obHoursRaw, 2);
                                if (obHours <= 0)
                                    continue;

                                var rate = obRates.FirstOrDefault(r => r.IsActive && r.Category == category);
                                if (rate == null)
                                    continue;

                                var pay = Math.Round(obHours * rate.RatePerHour, 2);

                                stats.TotalObHours += obHours;
                                stats.ObPay += pay;

                                stats.ObDetails.Add(new ObDetails
                                {
                                    Date = tempShift.ShiftDate,
                                    Hours = obHours,
                                    RatePerHour = rate.RatePerHour,
                                    Category = category,
                                    DayType = dayType,
                                    Pay = pay
                                });
                            }
                        }
                    }
                }

                activeHoursInPeriod = Math.Round(activeHoursInPeriod, 2);
                activeBasePay = Math.Round(activeBasePay, 2);

                stats.OnCallActivePay += activeBasePay;
                stats.OnCallActiveHours += activeHoursInPeriod;

                stats.TotalHours += activeHoursInPeriod;
                stats.BaseSalary += activeBasePay;

                stats.OnCallDetails.Add(new OnCallDetail
                {
                    Date = ws.ShiftDate,
                    StandbyStart = standbyStartDT,
                    StandbyEnd = standbyEndDT,
                    StandbyHours = standbyHours,
                    ActiveHours = activeHoursInPeriod,
                    StandbyPayType = ocs.StandbyPayTypeSnapshot,
                    StandbyPayAmount = ocs.StandbyPayAmountSnapshot,
                    StandbyPay = standbyPay,
                    ShiftNote = ocs.Notes,
                    Callouts = calloutDetails
                });
            }

            stats.OnCallPay = Math.Round(stats.OnCallPay, 2);
            stats.OnCallActivePay = Math.Round(stats.OnCallActivePay, 2);
            stats.OnCallStandbyHours = Math.Round(stats.OnCallStandbyHours, 2);
            stats.OnCallActiveHours = Math.Round(stats.OnCallActiveHours, 2);

            stats.TotalObHours = Math.Round(stats.TotalObHours, 2);
            stats.ObPay = Math.Round(stats.ObPay, 2);
        }

        private static List<(DateTime From, DateTime To)> SplitByMidnight(DateTime from, DateTime to)
        {
            var result = new List<(DateTime From, DateTime To)>();
            if (to <= from)
                return result;

            var cursor = from;

            while (cursor.Date < to.Date)
            {
                var nextMidnight = cursor.Date.AddDays(1);
                result.Add((cursor, nextMidnight));
                cursor = nextMidnight;
            }

            result.Add((cursor, to));
            return result;
        }

        private static decimal CalcStandbyPay(OnCallStandbyPayType type, decimal amount, decimal standbyHours)
        {
            return type switch
            {
                OnCallStandbyPayType.None => 0m,
                OnCallStandbyPayType.PerHour => Math.Round(standbyHours * amount, 2),
                OnCallStandbyPayType.PerShift => Math.Round(amount, 2),
                _ => 0m
            };
        }

        private static (DateTime start, DateTime end) BuildCalloutDateTimes(
            DateTime shiftDate,
            TimeSpan standbyStart,
            DateTime standbyStartDT,
            TimeSpan calloutStart,
            TimeSpan calloutEnd)
        {
            var startDT = shiftDate.Date.Add(calloutStart);
            if (startDT < standbyStartDT)
                startDT = startDT.AddDays(1);

            var endDT = shiftDate.Date.Add(calloutEnd);
            if (endDT <= shiftDate.Date.Add(calloutStart))
                endDT = endDT.AddDays(1);

            if (endDT < standbyStartDT)
                endDT = endDT.AddDays(1);

            if (endDT <= startDT)
                endDT = endDT.AddDays(1);

            return (startDT, endDT);
        }

        private static decimal OverlapHours(DateTime segStart, DateTime segEnd, DateTime periodStart, DateTime periodEnd)
        {
            var from = segStart < periodStart ? periodStart : segStart;
            var to = segEnd > periodEnd ? periodEnd : segEnd;
            if (to <= from)
                return 0m;
            return (decimal)(to - from).TotalHours;
        }

        #endregion
    }
}
