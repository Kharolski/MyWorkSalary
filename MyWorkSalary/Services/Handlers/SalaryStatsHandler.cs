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
        private readonly IOnCallRepository _onCallRepository;
        private readonly IOnCallCalloutRepository _onCallCalloutRepository;
        #endregion

        #region Constructor
        public SalaryStatsHandler(
            ISalaryRepository salaryRepository, 
            IOBEventRepository obEventRepository,
            IOnCallRepository onCallRepository,
            IOnCallCalloutRepository onCallCalloutRepository)
        {
            _salaryRepository = salaryRepository ?? throw new ArgumentNullException(nameof(salaryRepository));
            _obEventRepository = obEventRepository ?? throw new ArgumentNullException(nameof(obEventRepository));
            _onCallRepository = onCallRepository ?? throw new ArgumentNullException(nameof(onCallRepository));
            _onCallCalloutRepository = onCallCalloutRepository ?? throw new ArgumentNullException(nameof(onCallCalloutRepository));
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
        /// Sammanställer enkel statistik för en månad
        /// </summary>
        public SalaryStats CalculateMonthlyStats(int jobId, DateTime month)
        {
            var stats = new SalaryStats();

            var profile = _salaryRepository.GetJobProfile(jobId);
            if (profile == null)
                return stats;

            // month = utbetalningsmånad
            var payMonth = new DateTime(month.Year, month.Month, 1);

            // Regular (vanliga pass / grundlön)
            var regularWorkMonth = (profile.EmploymentType == EmploymentType.Permanent)
                ? payMonth
                : payMonth.AddMonths(-1);

            // Jour betalas alltid månaden efter den utförs
            var onCallWorkMonth = payMonth.AddMonths(-1);

            var (regularStart, regularEnd) = GetCalendarMonth(regularWorkMonth);
            var (onCallStart, onCallEnd) = GetCalendarMonth(onCallWorkMonth);

            // Hämta pass per period
            var regularShifts = _salaryRepository.GetShiftsForPeriod(jobId, regularStart, regularEnd)
                ?? Enumerable.Empty<WorkShift>();

            var onCallShifts = _salaryRepository.GetShiftsForPeriod(jobId, onCallStart, onCallEnd)
                ?? Enumerable.Empty<WorkShift>();

            // Filtrera
            var regularWorkShifts = regularShifts.Where(s => s.ShiftType != ShiftType.OnCall).ToList();
            var onCallWorkShifts = onCallShifts.Where(s => s.ShiftType == ShiftType.OnCall).ToList();

            // ===== NOLLSTÄLL =====
            stats.TotalHours = 0;
            stats.ExpectedHours = profile.ExpectedHoursPerMonth;
            stats.BaseSalary = 0;
            stats.TotalObHours = 0;
            stats.ObPay = 0;
            stats.ObDetails.Clear();
            stats.FlexBalance = 0;
            stats.SickDays = 0;
            stats.VacationDays = 0;
            stats.JourHours = 0;
            stats.OvertimePay = 0;
            stats.ExtraPay = 0;

            // Extra pass
            stats.ExtraShiftDetails.Clear();

            // ===== EXTRA PASS (ska följa regular-perioden) =====
            if (profile.ExtraShiftEnabled)
            {
                var extraShifts = regularWorkShifts
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

            // ===== GRUNDLÖN (regular-period) =====
            if (profile.EmploymentType == EmploymentType.Permanent)
            {
                stats.BaseSalary = profile.MonthlySalary ?? 0m;
            }
            else
            {
                var regularHours = regularWorkShifts
                    .Where(s => s.ShiftType == ShiftType.Regular)
                    .Sum(s => ShiftHoursSafe(s, regularStart, regularEnd));

                stats.BaseSalary = Math.Round(regularHours * (profile.HourlyRate ?? 0m), 2);
            }

            // ===== TOTALE TIMMAR (regular-period, utan jour-standby) =====
            stats.TotalHours = regularWorkShifts
                .Sum(s => ShiftHoursSafe(s, regularStart, regularEnd));

            // ===== OB (styrs av events i payMonth) =====
            // OB betalas alltid ut månaden EFTER arbetet
            var obWorkMonth = payMonth.AddMonths(-1);
            var (obWorkStart, obWorkEnd) = GetCalendarMonth(obWorkMonth);

            // shifts som OB bygger på (arbetsmånaden innan payMonth)
            var obSourceShifts = _salaryRepository
                .GetShiftsForPeriod(jobId, obWorkStart, obWorkEnd)
                ?? Enumerable.Empty<WorkShift>();

            // räkna aldrig OB på jour-standby via fallback
            obSourceShifts = obSourceShifts.Where(s => s.ShiftType != ShiftType.OnCall);

            CalculateOBFromEvents(jobId, payMonth, obSourceShifts, stats);

            // ===== JOUR (alltid onCall-perioden) =====
            // Viktigt: klipp mot onCallStart/onCallEnd, och skicka endast jourpassen
            ApplyOnCall(jobId, profile, onCallWorkShifts, onCallStart, onCallEnd, stats);

            // ===== SEMESTERERSÄTTNING (tim) =====
            if (profile.EmploymentType != EmploymentType.Permanent)
            {
                const decimal vacationRate = 0.12m;
                stats.VacationPay = Math.Round(stats.BaseSalary * vacationRate, 2);
            }
            else
            {
                stats.VacationPay = 0;
            }

            // ===== SKATT =====
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
                {
                    // Jour ska inte få OBHours på standby → sätt 0 (eller hoppa om du vill)
                    if (shift.ShiftType == ShiftType.OnCall)
                    {
                        shift.OBHours = 0;
                        continue;
                    }

                    shift.OBHours = obByShift.TryGetValue(shift.Id, out var h) ? h : 0;
                }

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
                // Fallback får aldrig räkna OB på jour-standby
                // Jour-aktiv OB hanteras i ApplyOnCall på callout-intervallen
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
                        IsDayMatch(rate, GetEffectiveDayForRate(rate, dayOfWeek, timeOfDay), shift.IsHoliday, shift.IsBigHoliday) &&
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

        #region OnCall / Jour
        private void ApplyOnCall(
             int jobId,
             JobProfile profile,
             IEnumerable<WorkShift> onCallShifts,   // <-- skicka in BARA jourpass
             DateTime periodStart,                  // <-- jour-periodens start (t.ex. 1 feb)
             DateTime periodEnd,                    // <-- jour-periodens slut  (t.ex. 1 mar)
             SalaryStats stats)
        {
            if (profile == null || !profile.OnCallEnabled)
                return;

            if (onCallShifts == null)
                return;

            // OB-rates behövs bara om du räknar OB på aktiv tid här
            var obRates = _salaryRepository
                .GetObShiftsForPeriod(jobId, DateTime.MinValue, DateTime.MaxValue)?
                .Where(r => r.IsActive)
                .ToList() ?? new List<OBRate>();

            foreach (var ws in onCallShifts)
            {
                // Säkerhet: vi jobbar bara med jourpass
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

                // ===== Callouts (aktiv tid) =====
                var callouts = _onCallCalloutRepository.GetByOnCallShiftId(ocs.Id) ?? new List<OnCallCallout>();
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

                    // RÄKNA ENDAST VIA SEGMENT (så split över midnatt blir korrekt)
                    foreach (var (segFrom, segTo) in SplitByMidnight(cStartDT, cEndDT))
                    {
                        var segHours = OverlapHours(segFrom, segTo, periodStart, periodEnd);
                        if (segHours <= 0)
                            continue;

                        segHours = Math.Round(segHours, 2);

                        // 1) summeringar
                        activeHoursInPeriod += segHours;

                        var segPay = Math.Round(segHours * ocs.ActiveHourlyRateSnapshot, 2);
                        activeBasePay += segPay;

                        // 2) UI-rad (split korrekt per datum)
                        calloutDetails.Add(new OnCallCalloutDetail
                        {
                            Date = segFrom.Date,
                            Start = segFrom.TimeOfDay,
                            End = segTo.TimeOfDay,
                            Hours = segHours,
                            Notes = c.Notes,
                            ActivePay = segPay
                        });

                        // 3) OB på aktiv tid (valfritt)
                        if (obRates.Count > 0)
                        {
                            var segDate = segFrom.Date;

                            // 1) Bestäm OB-dagtyp baserat på segmentets datum + användarens markeringar
                            var dayType = GetObDayType(segDate, ws.IsHoliday, ws.IsBigHoliday);

                            // 2) Skapa temporärt shift för OB-beräkning
                            var tempShift = new WorkShift
                            {
                                ShiftDate = segDate,
                                StartTime = segFrom,
                                EndTime = segTo,
                                IsHoliday = dayType == OBDayType.Holiday,
                                IsBigHoliday = dayType == OBDayType.BigHoliday
                            };

                            // 3) Räkna OB per kategori
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

                // Aktiv ersättning + timmar
                stats.OnCallActivePay += activeBasePay;
                stats.OnCallActiveHours += activeHoursInPeriod;

                // Aktiv tid räknas som arbetade timmar
                stats.TotalHours += activeHoursInPeriod;

                // Aktiv lön in i BaseSalary (så kort 2 stämmer)
                stats.BaseSalary += activeBasePay;

                // ===== EN detailrad per jourpass =====
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

            // Runda totals (stabil output)
            stats.OnCallPay = Math.Round(stats.OnCallPay, 2);
            stats.OnCallActivePay = Math.Round(stats.OnCallActivePay, 2);
            stats.OnCallStandbyHours = Math.Round(stats.OnCallStandbyHours, 2);
            stats.OnCallActiveHours = Math.Round(stats.OnCallActiveHours, 2);

            // Om du adderar OB här ska totals också rundas
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

        // Helpers
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

        // Bygger DateTime för callout inom jourpasset (hanterar över midnatt)
        private static (DateTime start, DateTime end) BuildCalloutDateTimes(
            DateTime shiftDate,
            TimeSpan standbyStart,
            DateTime standbyStartDT,
            TimeSpan calloutStart,
            TimeSpan calloutEnd)
        {
            // Start
            var startDT = shiftDate.Date.Add(calloutStart);
            if (startDT < standbyStartDT) // callout är efter midnatt
                startDT = startDT.AddDays(1);

            // End
            var endDT = shiftDate.Date.Add(calloutEnd);
            if (endDT <= shiftDate.Date.Add(calloutStart))
                endDT = endDT.AddDays(1);

            if (endDT < standbyStartDT)
                endDT = endDT.AddDays(1);

            // Extra säkerhet: om end <= start → bump
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

        private static DayOfWeek GetEffectiveDayForRate(OBRate rate, DayOfWeek currentDay, TimeSpan time)
        {
            // Om regeln går över midnatt (t.ex 22:00–06:00)
            // och vi är i "efter-midnatt"-delen (t.ex 00:15),
            // då ska vi matcha mot föregående dag.
            if (rate.StartTime > rate.EndTime && time < rate.EndTime)
            {
                return currentDay == DayOfWeek.Sunday ? DayOfWeek.Saturday : currentDay - 1;
            }

            return currentDay;
        }
        #endregion
    }
}
