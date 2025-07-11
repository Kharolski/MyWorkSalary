using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.Services.Handlers
{
    /// <summary>
    /// Hanterar all logik för sjukskrivning och sjuklöneberäkningar
    /// Stödjer både timanställda och fast anställda enligt svenska regler
    /// </summary>
    public class SickLeaveHandler
    {
        #region Private Fields
        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly ISickLeaveRepository _sickLeaveRepository;
        private readonly IShiftCalculationService _calculationService;
        #endregion

        #region Constructor
        public SickLeaveHandler(ISickLeaveRepository sickLeaveRepository, IWorkShiftRepository workShiftRepository, IShiftCalculationService calculationService)
        {
            _sickLeaveRepository = sickLeaveRepository;
            _workShiftRepository = workShiftRepository;
            _calculationService = calculationService;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Huvudmetod för att hantera sjukdag registrering
        /// Skapar både WorkShift och SickLeave med korrekt struktur
        /// </summary>
        public async Task<(WorkShift workShift, SickLeave sickLeave)> HandleSickLeave(
            DateTime date,
            JobProfile jobProfile,
            SickLeaveType sickType,
            TimeSpan? workedStartTime = null,
            TimeSpan? workedEndTime = null,
            TimeSpan? scheduledStartTime = null,
            TimeSpan? scheduledEndTime = null)
        {
            // 1. Kontrollera om användaren har rätt till sjuklön
            if (!HasRightToSickPay(jobProfile, date, sickType))
            {
                return await CreateNoPaySickLeave(date, jobProfile, sickType);
            }

            // 2. Kontrollera om det är första sjukdag i perioden (för karens)
            var isFirstSickDay = IsFirstSickDayInPeriod(jobProfile.Id, date);

            // 3. Hämta eller skapa SickPeriodId
            var sickPeriodId = await GetOrCreateSickPeriodId(jobProfile.Id, date, isFirstSickDay);

            // 4. Beräkna frysta värden (bara första gången i perioden)
            var frozenValues = await GetFrozenValuesForPeriod(jobProfile, sickPeriodId, isFirstSickDay);

            // 5. Skapa WorkShift och SickLeave baserat på sjuktyp
            return sickType switch
            {
                SickLeaveType.WorkedPartially => await CreatePartialSickLeave(
                    date, jobProfile, sickPeriodId, frozenValues, isFirstSickDay,
                    workedStartTime.Value, workedEndTime.Value,
                    scheduledStartTime.Value, scheduledEndTime.Value),

                SickLeaveType.ShouldHaveWorked => await CreateFullSickLeave(
                    date, jobProfile, sickPeriodId, frozenValues, isFirstSickDay,
                    scheduledStartTime.Value, scheduledEndTime.Value),

                SickLeaveType.WouldBeFree => await CreateNoPaySickLeave(date, jobProfile, sickType),

                _ => throw new ArgumentException($"Okänd sjuktyp: {sickType}")
            };
        }

        /// <summary>
        /// Beräknar sjuklön och karensinfo för UI-visning
        /// </summary>
        public async Task<SickPayCalculationResult> CalculateSickPayForUI(
            JobProfile jobProfile,
            SickLeaveType sickType,
            TimeSpan? workedHours = null,
            TimeSpan? scheduledHours = null)
        {
            if (jobProfile == null)
            {
                return new SickPayCalculationResult
                {
                    TotalPay = 0,
                    SickPay = 0,
                    RegularPay = 0,
                    KarensDeduction = 0,
                    HasKarensDeduction = false,
                    ErrorMessage = "Ingen jobbprofil"
                };
            }

            try
            {
                // Kontrollera rätt till sjuklön
                if (!HasRightToSickPay(jobProfile, DateTime.Today, sickType))
                {
                    return new SickPayCalculationResult
                    {
                        TotalPay = 0,
                        SickPay = 0,
                        RegularPay = 0,
                        KarensDeduction = 0,
                        HasKarensDeduction = false,
                        ErrorMessage = "Ingen rätt till sjuklön för denna dag"
                    };
                }

                // Kontrollera om första sjukdag
                var isFirstSickDay = IsFirstSickDayInPeriod(jobProfile.Id, DateTime.Today);

                // Hämta frysta värden för beräkning
                var frozenValues = await GetFrozenValuesForPeriod(jobProfile, 0, isFirstSickDay);

                decimal regularPay = 0;
                decimal sickPay = 0;
                decimal sickHours = 0;

                switch (sickType)
                {
                    case SickLeaveType.WorkedPartially:
                        if (workedHours.HasValue && scheduledHours.HasValue)
                        {
                            var worked = (decimal)workedHours.Value.TotalHours;
                            var scheduled = (decimal)scheduledHours.Value.TotalHours;
                            sickHours = scheduled - worked;

                            // Vanlig lön för arbetade timmar
                            regularPay = (frozenValues.HourlyRateUsed) * worked;

                            // Sjuklön för sjuka timmar (80% av timlön)
                            sickPay = (frozenValues.HourlyRateUsed) * 0.8m * sickHours;
                        }
                        break;

                    case SickLeaveType.ShouldHaveWorked:
                        if (scheduledHours.HasValue)
                        {
                            sickHours = (decimal)scheduledHours.Value.TotalHours;
                        }
                        else
                        {
                            // Default 8 timmar för hel dag
                            sickHours = 8;
                        }
                        // Sjuklön för alla timmar (80% av timlön)
                        sickPay = (frozenValues.HourlyRateUsed) * 0.8m * sickHours;
                        break;

                    case SickLeaveType.WouldBeFree:
                        // Ingen betalning
                        break;
                }

                return new SickPayCalculationResult
                {
                    TotalPay = regularPay + sickPay - frozenValues.KarensDeduction,
                    SickPay = sickPay,
                    RegularPay = regularPay,
                    KarensDeduction = frozenValues.KarensDeduction,
                    HasKarensDeduction = isFirstSickDay && frozenValues.KarensDeduction > 0,
                    SickHours = sickHours,
                    ErrorMessage = null
                };
            }
            catch (Exception ex)
            {
                return new SickPayCalculationResult
                {
                    TotalPay = 0,
                    SickPay = 0,
                    RegularPay = 0,
                    KarensDeduction = 0,
                    HasKarensDeduction = false,
                    ErrorMessage = $"Fel vid beräkning: {ex.Message}"
                };
            }
        }
        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Kontrollerar om det är första sjukdag i en sjukperiod
        /// Samma period = inom 5 kalenderdagar från senaste sjukdag
        /// </summary>
        private bool IsFirstSickDayInPeriod(int jobProfileId, DateTime date)
        {
            var fiveDaysAgo = date.AddDays(-5);
            var recentSickDays = _sickLeaveRepository.GetRecentSickLeaves(jobProfileId, fiveDaysAgo, 5);

            return !recentSickDays.Any(); // Första dagen om inga sjukdagar senaste 5 dagarna
        }

        /// <summary>
        /// Hämtar eller skapar SickPeriodId för gruppering av sjukdagar
        /// </summary>
        private async Task<int> GetOrCreateSickPeriodId(int jobProfileId, DateTime date, bool isFirstSickDay)
        {
            if (isFirstSickDay)
            {
                // Ny sjukperiod - skapa nytt ID
                return _sickLeaveRepository.GetNextSickPeriodId();
            }
            else
            {
                // Fortsättning på befintlig period - hitta senaste
                var fiveDaysAgo = date.AddDays(-5);
                var recentSickLeaves = _sickLeaveRepository.GetRecentSickLeaves(jobProfileId, fiveDaysAgo, 5);
                return recentSickLeaves.FirstOrDefault()?.SickPeriodId ?? _sickLeaveRepository.GetNextSickPeriodId();
            }
        }

        /// <summary>
        /// Hämtar frysta värden för sjukperioden (beräknas bara första gången)
        /// </summary>
        private async Task<FrozenSickValues> GetFrozenValuesForPeriod(JobProfile jobProfile, int sickPeriodId, bool isFirstSickDay)
        {
            if (!isFirstSickDay && sickPeriodId > 0)
            {
                // Hämta befintliga frysta värden från första dagen i perioden
                var existingSickLeaves = _sickLeaveRepository.GetSickLeavesByPeriodId(sickPeriodId);
                var firstSickLeave = existingSickLeaves.FirstOrDefault();

                if (firstSickLeave != null)
                {
                    return new FrozenSickValues
                    {
                        WeeklyHoursUsed = firstSickLeave.WeeklyHoursUsed ?? 0,
                        HourlyRateUsed = firstSickLeave.HourlyRateUsed ?? 0,
                        WeeklyEarningsUsed = firstSickLeave.WeeklyEarningsUsed ?? 0,
                        KarensDeduction = 0 // Bara första dagen
                    };
                }
            }

            // Beräkna nya frysta värden för första dagen i perioden
            var weeklyHours = await GetAverageWeeklyHours(jobProfile);
            var hourlyRate = jobProfile.EmploymentType == EmploymentType.Permanent
                ? _calculationService.CalculateHourlyRateFromMonthlySalary(jobProfile)
                : jobProfile.HourlyRate ?? 0;

            var weeklyEarnings = weeklyHours * hourlyRate;
            var karensDeduction = isFirstSickDay ? weeklyEarnings * 0.8m * 0.2m : 0; // 20% av sjuklön

            return new FrozenSickValues
            {
                WeeklyHoursUsed = weeklyHours,
                HourlyRateUsed = hourlyRate,
                WeeklyEarningsUsed = weeklyEarnings,
                KarensDeduction = karensDeduction
            };
        }

        /// <summary>
        /// Skapar WorkShift och SickLeave för delvis sjukdag
        /// </summary>
        private async Task<(WorkShift, SickLeave)> CreatePartialSickLeave(
            DateTime date, JobProfile jobProfile, int sickPeriodId,
            FrozenSickValues frozenValues, bool isFirstSickDay,
            TimeSpan workedStartTime, TimeSpan workedEndTime,
            TimeSpan scheduledStartTime, TimeSpan scheduledEndTime)
        {
            var workedHours = (decimal)(workedEndTime - workedStartTime).TotalHours;
            var scheduledHours = (decimal)(scheduledEndTime - scheduledStartTime).TotalHours;
            var sickHours = scheduledHours - workedHours;

            // Beräkna betalning
            var regularPay = frozenValues.HourlyRateUsed * workedHours;
            var sickPay = frozenValues.HourlyRateUsed * 0.8m * sickHours;
            var totalPay = regularPay + sickPay - frozenValues.KarensDeduction;

            // Skapa WorkShift
            var workShift = new WorkShift
            {
                JobProfileId = jobProfile.Id,
                ShiftDate = date,
                ShiftType = ShiftType.SickLeave,
                StartTime = date.Add(scheduledStartTime),
                EndTime = date.Add(scheduledEndTime),
                RegularHours = workedHours,
                RegularPay = regularPay,
                TotalHours = scheduledHours,
                TotalPay = totalPay,
                Notes = $"Delvis sjuk - Jobbat {workedHours:F1}h, sjuk {sickHours:F1}h"
            };

            // Spara WorkShift först för att få ID
            var savedWorkShift = await _workShiftRepository.SaveWorkShiftAsync(workShift);

            // Skapa SickLeave
            var sickLeave = new SickLeave
            {
                WorkShiftId = savedWorkShift.Id,
                SickType = SickLeaveType.WorkedPartially,
                SickPeriodId = sickPeriodId,
                IsRecurrentSickPeriod = !isFirstSickDay,

                // Frysta värden
                WeeklyHoursUsed = frozenValues.WeeklyHoursUsed,
                HourlyRateUsed = frozenValues.HourlyRateUsed,
                WeeklyEarningsUsed = frozenValues.WeeklyEarningsUsed,
                KarensDeduction = frozenValues.KarensDeduction,
                DailySickEarnings = sickPay,

                // Arbetstider
                WorkedStartTime = workedStartTime,
                WorkedEndTime = workedEndTime,
                ScheduledStartTime = scheduledStartTime,
                ScheduledEndTime = scheduledEndTime,

                CreatedDate = DateTime.Now
            };

            // Spara SickLeave
            await _sickLeaveRepository.SaveSickLeaveAsync(sickLeave);

            return (savedWorkShift, sickLeave);
        }

        /// <summary>
        /// Skapar WorkShift och SickLeave för hel sjukdag
        /// </summary>
        private async Task<(WorkShift, SickLeave)> CreateFullSickLeave(
            DateTime date, JobProfile jobProfile, int sickPeriodId,
            FrozenSickValues frozenValues, bool isFirstSickDay,
            TimeSpan scheduledStartTime, TimeSpan scheduledEndTime)
        {
            var scheduledHours = (decimal)(scheduledEndTime - scheduledStartTime).TotalHours;
            var sickPay = frozenValues.HourlyRateUsed * 0.8m * scheduledHours;
            var totalPay = sickPay - frozenValues.KarensDeduction;

            // Skapa WorkShift
            var workShift = new WorkShift
            {
                JobProfileId = jobProfile.Id,
                ShiftDate = date,
                ShiftType = ShiftType.SickLeave,
                StartTime = date.Add(scheduledStartTime),
                EndTime = date.Add(scheduledEndTime),
                RegularHours = 0,
                RegularPay = 0,
                TotalHours = scheduledHours,
                TotalPay = totalPay,
                Notes = $"Sjukdag - {scheduledHours:F1}t" + (isFirstSickDay ? " (med karensavdrag)" : "")
            };

            // Spara WorkShift först
            var savedWorkShift = await _workShiftRepository.SaveWorkShiftAsync(workShift);

            // Skapa SickLeave
            var sickLeave = new SickLeave
            {
                WorkShiftId = savedWorkShift.Id,
                SickType = SickLeaveType.ShouldHaveWorked,
                SickPeriodId = sickPeriodId,
                IsRecurrentSickPeriod = !isFirstSickDay,

                // Frysta värden
                WeeklyHoursUsed = frozenValues.WeeklyHoursUsed,
                HourlyRateUsed = frozenValues.HourlyRateUsed,
                WeeklyEarningsUsed = frozenValues.WeeklyEarningsUsed,
                KarensDeduction = frozenValues.KarensDeduction,
                DailySickEarnings = sickPay,

                // Arbetstider (bara schemalagda för hel sjukdag)
                ScheduledStartTime = scheduledStartTime,
                ScheduledEndTime = scheduledEndTime,

                CreatedDate = DateTime.Now
            };

            // Spara SickLeave
            await _sickLeaveRepository.SaveSickLeaveAsync(sickLeave);

            return (savedWorkShift, sickLeave);
        }

        /// <summary>
        /// Skapar WorkShift och SickLeave för sjukdag utan betalning
        /// </summary>
        private async Task<(WorkShift, SickLeave)> CreateNoPaySickLeave(
            DateTime date, JobProfile jobProfile, SickLeaveType sickType)
        {
            // Skapa WorkShift utan betalning
            var workShift = new WorkShift
            {
                JobProfileId = jobProfile.Id,
                ShiftDate = date,
                ShiftType = ShiftType.SickLeave,
                StartTime = date,
                EndTime = date,
                RegularHours = 0,
                RegularPay = 0,
                TotalHours = 0,
                TotalPay = 0,
                Notes = "Sjukdag - skulle varit ledig (ingen ersättning)"
            };

            // Spara WorkShift först
            var savedWorkShift = await _workShiftRepository.SaveWorkShiftAsync(workShift);

            // Skapa SickLeave (för spårning även utan betalning)
            var sickLeave = new SickLeave
            {
                WorkShiftId = savedWorkShift.Id,
                SickType = sickType,
                SickPeriodId = null, // Ingen period för obetald sjukdag
                IsRecurrentSickPeriod = false,

                // Inga frysta värden för obetald sjukdag
                WeeklyHoursUsed = 0,
                HourlyRateUsed = 0,
                WeeklyEarningsUsed = 0,
                KarensDeduction = 0,
                DailySickEarnings = 0,

                CreatedDate = DateTime.Now
            };

            // Spara SickLeave
            await _sickLeaveRepository.SaveSickLeaveAsync(sickLeave);

            return (savedWorkShift, sickLeave);
        }

        /// <summary>
        /// Kontrollerar om användaren har rätt till sjuklön från arbetsgivaren
        /// </summary>
        public bool HasRightToSickPay(JobProfile jobProfile, DateTime date, SickLeaveType sickType)
        {
            // Om skulle varit ledig = ingen sjuklön
            if (sickType == SickLeaveType.WouldBeFree)
                return false;

            if (jobProfile.EmploymentType == EmploymentType.Permanent)
            {
                // Fast anställd har alltid rätt om skulle jobbat
                return sickType == SickLeaveType.ShouldHaveWorked || sickType == SickLeaveType.WorkedPartially;
            }
            else
            {
                // Timanställd: Kontrollera om var inbokad
                return WasScheduledToWork(jobProfile.Id, date);
            }
        }

        /// <summary>
        /// Kontrollerar om timanställd var schemalagd att jobba på specifikt datum
        /// </summary>
        private bool WasScheduledToWork(int jobProfileId, DateTime date)
        {
            var existingShift = _workShiftRepository.GetWorkShiftsForDate(jobProfileId, date);
            return existingShift.Any();
        }

        /// <summary>
        /// Hämtar genomsnittliga arbetstimmar per vecka för timanställd
        /// </summary>
        private async Task<decimal> GetAverageWeeklyHours(JobProfile jobProfile)
        {
            // Försök beräkna från historik (senaste 13 veckor)
            var historicalAverage = CalculateHistoricalAverage(jobProfile.Id);
            if (historicalAverage > 0)
                return historicalAverage;

            // Om ingen historik finns, fråga användaren
            return await AskUserForWeeklyAverage();
        }

        /// <summary>
        /// Beräknar genomsnittliga timmar per vecka från senaste 13 veckor
        /// </summary>
        private decimal CalculateHistoricalAverage(int jobProfileId)
        {
            var thirteenWeeksAgo = DateTime.Today.AddDays(-91);
            var shifts = _workShiftRepository.GetWorkShiftsForDateRange(jobProfileId, thirteenWeeksAgo, DateTime.Today)
                .Where(s => s.ShiftType == ShiftType.Regular)
                .ToList();

            if (shifts.Count == 0)
                return 0;

            var totalHours = shifts.Sum(s => s.TotalHours);
            return totalHours / 13; // Genomsnitt per vecka
        }

        /// <summary>
        /// Frågar användaren om genomsnittliga arbetstimmar per vecka
        /// </summary>
        private async Task<decimal> AskUserForWeeklyAverage()
        {
            // TODO: Implementera popup/dialog för att fråga användaren
            return 30; // Placeholder
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// Frysta värden för sjukperiod (beräknas bara första gången)
    /// </summary>
    public class FrozenSickValues
    {
        public decimal WeeklyHoursUsed { get; set; }
        public decimal HourlyRateUsed { get; set; }
        public decimal WeeklyEarningsUsed { get; set; }
        public decimal KarensDeduction { get; set; }
    }

    /// <summary>
    /// Resultat från sjuklöneberäkning för UI-visning
    /// </summary>
    public class SickPayCalculationResult
    {
        public decimal TotalPay { get; set; }
        public decimal SickPay { get; set; }
        public decimal RegularPay { get; set; }
        public decimal KarensDeduction { get; set; }
        public bool HasKarensDeduction { get; set; }
        public decimal SickHours { get; set; }
        public string ErrorMessage { get; set; }
    }

    #endregion

}


