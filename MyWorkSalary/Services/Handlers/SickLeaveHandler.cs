using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
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
        private readonly DatabaseService _databaseService;
        private readonly ISickLeaveRepository _sickLeaveRepository;
        private readonly IShiftCalculationService _calculationService;
        #endregion

        #region Constructor
        /// <summary>
        /// Initierar SickLeaveHandler med nödvändiga services
        /// </summary>
        /// <param name="databaseService">Databas service för att hämta/spara data</param>
        /// <param name="calculationService">Service för löneberäkningar</param>
        public SickLeaveHandler(DatabaseService databaseService, ISickLeaveRepository sickLeaveRepository, IShiftCalculationService calculationService)
        {
            _databaseService = databaseService;
            _sickLeaveRepository = sickLeaveRepository;
            _calculationService = calculationService;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Huvudmetod för att hantera sjukdag registrering
        /// Skapar WorkShift med korrekt sjuklön baserat på anställningstyp och situation
        /// </summary>
        /// <param name="date">Datum för sjukdagen</param>
        /// <param name="jobProfile">Användarens jobbprofil</param>
        /// <param name="sickType">Typ av sjukdag (jobbat delvis, skulle jobbat, skulle varit ledig)</param>
        /// <param name="workedHours">Timmar som faktiskt jobbades (om delvis sjuk)</param>
        /// <param name="scheduledHours">Timmar som skulle jobbats totalt</param>
        /// <returns>WorkShift med beräknad sjuklön</returns>
        public async Task<WorkShift> HandleSickLeave(
            DateTime date,
            JobProfile jobProfile,
            SickLeaveType sickType,
            TimeSpan? workedHours = null,
            TimeSpan? scheduledHours = null)
        {
            System.Diagnostics.Debug.WriteLine($"🏥 HandleSickLeave - Sparar sjukdag:");
            System.Diagnostics.Debug.WriteLine($"   Datum: {date:yyyy-MM-dd}");
            System.Diagnostics.Debug.WriteLine($"   SickType: {sickType}");
            System.Diagnostics.Debug.WriteLine($"   JobProfile: {jobProfile?.JobTitle}");

            // Kontrollera om användaren har rätt till sjuklön
            if (!HasRightToSickPay(jobProfile, date, sickType))
            {
                var noPayShift = CreateNoPaySickLeave(date, jobProfile);

                // 🔧 SPARA I DATABASEN
                var savedNoPayShift = _databaseService.WorkShifts.SaveWorkShift(noPayShift);
                System.Diagnostics.Debug.WriteLine($"✅ NoPaySickLeave sparad: ID={savedNoPayShift.Id}");

                return savedNoPayShift;
            }

            // Kontrollera om det är första sjukdag i perioden
            var isFirstSickDay = IsFirstSickDayInPeriod(jobProfile.Id, date);
            System.Diagnostics.Debug.WriteLine($"   IsFirstSickDay: {isFirstSickDay}");

            // Skapa WorkShift baserat på sjuktyp
            WorkShift workShift = sickType switch
            {
                SickLeaveType.WorkedPartially => await CreatePartialSickLeave(date, jobProfile, workedHours.Value, scheduledHours.Value, isFirstSickDay),
                SickLeaveType.ShouldHaveWorked => await CreateFullSickLeave(date, jobProfile, scheduledHours.Value, isFirstSickDay),
                SickLeaveType.WouldBeFree => CreateNoPaySickLeave(date, jobProfile),
                _ => throw new ArgumentException($"Okänd sjuktyp: {sickType}")
            };

            System.Diagnostics.Debug.WriteLine($"🏥 WorkShift skapad:");
            System.Diagnostics.Debug.WriteLine($"   TotalHours: {workShift.TotalHours}");
            System.Diagnostics.Debug.WriteLine($"   SickPay: {workShift.SickPay}");
            System.Diagnostics.Debug.WriteLine($"   TotalPay: {workShift.TotalPay}");

            // 🔧 SPARA I DATABASEN
            var savedWorkShift = _databaseService.WorkShifts.SaveWorkShift(workShift);
            System.Diagnostics.Debug.WriteLine($"✅ SickLeave sparad i databas: ID={savedWorkShift.Id}");

            return savedWorkShift;
        }

        /// <summary>
        /// Beräknar karensavdrag för sjukperiod
        /// Karensavdrag = 20% av veckoersättning i sjuklön (80% av normal lön)
        /// </summary>
        /// <param name="jobProfile">Användarens jobbprofil</param>
        /// <param name="isFirstSickDay">Om det är första sjukdagen i perioden</param>
        /// <returns>Karensavdrag i kronor, 0 om inte första dagen</returns>
        public async Task<decimal> CalculateKarensavdrag(JobProfile jobProfile, bool isFirstSickDay)
        {
            if (!isFirstSickDay)
                return 0;

            decimal weeklyEarnings;

            if (jobProfile.EmploymentType == EmploymentType.Permanent)
            {
                // Fast anställd: (Månadslön × 12) ÷ 52
                weeklyEarnings = (jobProfile.MonthlySalary ?? 0) * 12 / 52;
            }
            else
            {
                // Timanställd: Timlön × genomsnittliga timmar per vecka
                var averageWeeklyHours = await GetAverageWeeklyHours(jobProfile);
                weeklyEarnings = (jobProfile.HourlyRate ?? 0) * averageWeeklyHours;
            }

            // Sjuklön = 80% av veckolön, Karensavdrag = 20% av sjuklön
            var sickPayWeekly = weeklyEarnings * 0.8m;
            var karensavdrag = sickPayWeekly * 0.2m;

            return karensavdrag;
        }

        /// <summary>
        /// Kontrollerar om användaren har rätt till sjuklön från arbetsgivaren
        /// Timanställd: Bara om inbokad för dagen
        /// Fast anställd: Alltid om skulle jobbat
        /// </summary>
        /// <param name="jobProfile">Användarens jobbprofil</param>
        /// <param name="date">Datum för sjukdagen</param>
        /// <param name="sickType">Typ av sjukdag</param>
        /// <returns>True om rätt till sjuklön, annars false</returns>
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
        /// Beräknar sjuklön och karensinfo för UI-visning
        /// </summary>
        /// <param name="jobProfile">Jobbprofil</param>
        /// <param name="sickType">Typ av sjukdag</param>
        /// <param name="workedHours">Arbetstimmar (för delvis sjuk)</param>
        /// <param name="scheduledHours">Schemalagda timmar</param>
        /// <returns>Beräkningsresultat för UI</returns>
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
                            regularPay = jobProfile.EmploymentType == EmploymentType.Permanent
                                ? CalculateMonthlyProportionalPay(jobProfile, worked)
                                : (jobProfile.HourlyRate ?? 0) * worked;

                            // Sjuklön för sjuka timmar
                            sickPay = await CalculateSickPay(jobProfile, sickHours);
                        }
                        break;

                    case SickLeaveType.ShouldHaveWorked:
                        if (scheduledHours.HasValue)
                        {
                            sickHours = (decimal)scheduledHours.Value.TotalHours;
                            sickPay = await CalculateSickPay(jobProfile, sickHours);
                        }
                        else
                        {
                            // Default 8 timmar för hel dag
                            sickHours = 8;
                            sickPay = await CalculateSickPay(jobProfile, 8);
                        }
                        break;

                    case SickLeaveType.WouldBeFree:
                        // Ingen betalning
                        break;
                }

                // Beräkna karensavdrag
                var karensDeduction = await CalculateKarensavdrag(jobProfile, isFirstSickDay);

                return new SickPayCalculationResult
                {
                    TotalPay = regularPay + sickPay - karensDeduction,
                    SickPay = sickPay,
                    RegularPay = regularPay,
                    KarensDeduction = karensDeduction,
                    HasKarensDeduction = isFirstSickDay && karensDeduction > 0,
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
        /// <param name="jobProfileId">ID för jobbprofil</param>
        /// <param name="date">Datum att kontrollera</param>
        /// <returns>True om första dagen i ny period</returns>
        private bool IsFirstSickDayInPeriod(int jobProfileId, DateTime date)
        {
            var fiveDaysAgo = date.AddDays(-5);
            var recentSickDays = _databaseService.WorkShifts.GetWorkShifts(jobProfileId)
                .Where(s => s.ShiftType == ShiftType.SickLeave &&
                           s.ShiftDate >= fiveDaysAgo &&
                           s.ShiftDate < date)
                .Any();

            return !recentSickDays; // Första dagen om inga sjukdagar senaste 5 dagarna
        }

        /// <summary>
        /// Hämtar genomsnittliga arbetstimmar per vecka för timanställd
        /// Försöker först beräkna från senaste 13 veckor, annars frågar användaren
        /// </summary>
        /// <param name="jobProfile">Jobbprofil för timanställd</param>
        /// <returns>Genomsnittliga timmar per vecka</returns>
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
        /// <param name="jobProfileId">ID för jobbprofil</param>
        /// <returns>Genomsnitt timmar/vecka, 0 om ingen data</returns>
        private decimal CalculateHistoricalAverage(int jobProfileId)
        {
            var thirteenWeeksAgo = DateTime.Today.AddDays(-91); // 13 veckor = 91 dagar
            var shifts = _databaseService.WorkShifts.GetWorkShifts(jobProfileId)
                .Where(s => s.ShiftDate >= thirteenWeeksAgo &&
                           s.ShiftType == ShiftType.Regular)
                .ToList();

            if (shifts.Count == 0)
                return 0;

            var totalHours = shifts.Sum(s => s.TotalHours);
            return totalHours / 13; // Genomsnitt per vecka
        }

        /// <summary>
        /// Frågar användaren om genomsnittliga arbetstimmar per vecka
        /// Används när ingen historisk data finns
        /// </summary>
        /// <returns>Användarens uppskattade timmar per vecka</returns>
        private async Task<decimal> AskUserForWeeklyAverage()
        {
            // TODO: Implementera popup/dialog för att fråga användaren
            // Returnerar hårdkodat värde för nu
            return 30; // Placeholder - ska ersättas med faktisk dialog
        }

        /// <summary>
        /// Kontrollerar om timanställd var schemalagd att jobba på specifikt datum
        /// </summary>
        /// <param name="jobProfileId">ID för jobbprofil</param>
        /// <param name="date">Datum att kontrollera</param>
        /// <returns>True om var schemalagd</returns>
        private bool WasScheduledToWork(int jobProfileId, DateTime date)
        {
            // Kontrollera om det finns ett registrerat pass för dagen
            var existingShift = _databaseService.WorkShifts.GetWorkShifts(jobProfileId)
                .FirstOrDefault(s => s.ShiftDate.Date == date.Date);

            return existingShift != null;
        }

        /// <summary>
        /// Skapar WorkShift för delvis sjukdag (jobbat en del, sjuk resten)
        /// </summary>
        /// <param name="date">Datum</param>
        /// <param name="jobProfile">Jobbprofil</param>
        /// <param name="workedHours">Timmar som jobbades</param>
        /// <param name="scheduledHours">Totala schemalagda timmar</param>
        /// <param name="isFirstSickDay">Om första sjukdag i period</param>
        /// <returns>WorkShift med kombinerad lön och sjuklön</returns>
        private async Task<WorkShift> CreatePartialSickLeave(
            DateTime date,
            JobProfile jobProfile,
            TimeSpan workedHours,
            TimeSpan scheduledHours,
            bool isFirstSickDay)
        {
            var sickHours = scheduledHours - workedHours;
            var sickPay = await CalculateSickPay(jobProfile, (decimal)sickHours.TotalHours);
            var karensavdrag = await CalculateKarensavdrag(jobProfile, isFirstSickDay);

            // Beräkna vanlig lön för jobbade timmar
            var regularPay = jobProfile.EmploymentType == EmploymentType.Permanent
                ? CalculateMonthlyProportionalPay(jobProfile, (decimal)workedHours.TotalHours)
                : jobProfile.HourlyRate * (decimal)workedHours.TotalHours;

            return new WorkShift
            {
                JobProfileId = jobProfile.Id,
                ShiftDate = date,
                ShiftType = ShiftType.SickLeave,
                StartTime = DateTime.Today, // Kan justeras baserat på UI
                EndTime = DateTime.Today.Add(scheduledHours),
                RegularHours = (decimal)workedHours.TotalHours,
                SickHours = (decimal)sickHours.TotalHours,
                RegularPay = regularPay ?? 0,
                SickPay = sickPay - karensavdrag,
                TotalPay = (regularPay ?? 0) + (sickPay - karensavdrag),
                TotalHours = (decimal)scheduledHours.TotalHours,
                Notes = $"Delvis sjuk - Jobbat {workedHours.TotalHours:F1}h, sjuk {sickHours.TotalHours:F1}h"
            };
        }

        /// <summary>
        /// Skapar WorkShift för hel sjukdag (skulle jobbat men var sjuk)
        /// </summary>
        /// <param name="date">Datum</param>
        /// <param name="jobProfile">Jobbprofil</param>
        /// <param name="scheduledHours">Timmar som skulle jobbats</param>
        /// <param name="isFirstSickDay">Om första sjukdag i period</param>
        /// <returns>WorkShift med bara sjuklön</returns>
        private async Task<WorkShift> CreateFullSickLeave(
            DateTime date,
            JobProfile jobProfile,
            TimeSpan scheduledHours,
            bool isFirstSickDay)
        {
            var sickPay = await CalculateSickPay(jobProfile, (decimal)scheduledHours.TotalHours);
            var karensavdrag = await CalculateKarensavdrag(jobProfile, isFirstSickDay);

            return new WorkShift
            {
                JobProfileId = jobProfile.Id,
                ShiftDate = date,
                ShiftType = ShiftType.SickLeave,
                StartTime = DateTime.Today,
                EndTime = DateTime.Today.Add(scheduledHours),
                RegularHours = 0,
                SickHours = (decimal)scheduledHours.TotalHours,
                RegularPay = 0,
                SickPay = sickPay - karensavdrag,
                TotalPay = sickPay - karensavdrag,
                TotalHours = (decimal)scheduledHours.TotalHours,
                Notes = $"Sjukdag - {scheduledHours.TotalHours:F1}t" + (isFirstSickDay ? " (med karensavdrag)" : "")
            };
        }

        /// <summary>
        /// Skapar WorkShift för sjukdag utan betalning (skulle varit ledig)
        /// </summary>
        /// <param name="date">Datum</param>
        /// <param name="jobProfile">Jobbprofil</param>
        /// <returns>WorkShift utan betalning</returns>
        private WorkShift CreateNoPaySickLeave(DateTime date, JobProfile jobProfile)
        {
            return new WorkShift
            {
                JobProfileId = jobProfile.Id,
                ShiftDate = date,
                ShiftType = Models.Enums.ShiftType.SickLeave,
                StartTime = DateTime.Today,
                EndTime = DateTime.Today,
                RegularHours = 0,
                SickHours = 0,
                RegularPay = 0,
                SickPay = 0,
                TotalPay = 0,
                TotalHours = 0,
                Notes = "Sjukdag - skulle varit ledig (ingen ersättning)"
            };
        }

        /// <summary>
        /// Beräknar sjuklön (80% av normal lön) för angivet antal timmar
        /// </summary>
        /// <param name="jobProfile">Jobbprofil</param>
        /// <param name="sickHours">Antal sjuktimmar</param>
        /// <returns>Sjuklön i kronor</returns>
        private async Task<decimal> CalculateSickPay(JobProfile jobProfile, decimal sickHours)
        {
            decimal hourlyRate;

            if (jobProfile.EmploymentType == EmploymentType.Permanent)
            {
                // Beräkna timlön från månadslön
                hourlyRate = CalculateHourlyRateFromMonthlySalary(jobProfile);
            }
            else
            {
                hourlyRate = jobProfile.HourlyRate ?? 0;
            }

            // Sjuklön = 80% av normal timlön
            return hourlyRate * 0.8m * sickHours;
        }

        /// <summary>
        /// Beräknar timlön från månadslön för fast anställda
        /// </summary>
        /// <param name="jobProfile">Jobbprofil</param>
        /// <returns>Timlön baserad på månadslön</returns>
        private decimal CalculateHourlyRateFromMonthlySalary(JobProfile jobProfile)
        {
            return _calculationService.CalculateHourlyRateFromMonthlySalary(jobProfile);
        }

        /// <summary>
        /// Beräknar proportionell månadslön för angivet antal timmar
        /// </summary>
        /// <param name="jobProfile">Jobbprofil</param>
        /// <param name="workedHours">Antal arbetade timmar</param>
        /// <returns>Proportionell lön</returns>
        private decimal CalculateMonthlyProportionalPay(JobProfile jobProfile, decimal workedHours)
        {
            var hourlyRate = CalculateHourlyRateFromMonthlySalary(jobProfile);
            return hourlyRate * workedHours;
        }
        #endregion
    }

    #region Enums
    /// <summary>
    /// Typ av sjukdag som användaren registrerar
    /// </summary>
    public enum SickLeaveType
    {
        /// <summary>
        /// Har jobbat delvis - får vanlig lön + sjuklön
        /// </summary>
        WorkedPartially,

        /// <summary>
        /// Skulle ha jobbat - får bara sjuklön
        /// </summary>
        ShouldHaveWorked,

        /// <summary>
        /// Skulle varit ledig - ingen ersättning
        /// </summary>
        WouldBeFree
    }
    #endregion

    #region Result Classes (lägg till i slutet av filen)

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

