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
    public partial class SalaryStatsHandler
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
        // Flyttad till partial-fil: SalaryStatsHandler.OB.cs
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
        // Flyttad till partial-fil: SalaryStatsHandler.OnCall.cs
        #endregion
    }
}
