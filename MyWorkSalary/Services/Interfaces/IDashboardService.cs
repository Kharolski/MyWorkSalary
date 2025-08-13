using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IDashboardService
    {
        #region Existing Methods
        /// <summary>
        /// Hämta månadens statistik för ett jobb
        /// </summary>
        /// <param name="jobProfileId">ID för jobbprofil</param>
        /// <returns>Statistik för aktuell kalendermånad</returns>
        MonthlyStats GetMonthlyStats(int jobProfileId);

        /// <summary>
        /// Hämta senaste aktiviteter för dashboard
        /// </summary>
        /// <param name="jobProfileId">ID för jobbprofil</param>
        /// <param name="count">Antal aktiviteter att hämta (default 4)</param>
        /// <returns>Lista med senaste aktiviteter</returns>
        List<RecentActivityItem> GetRecentActivities(int jobProfileId, int count = 4);

        /// <summary>
        /// Hämta aktivt jobb
        /// </summary>
        /// <returns>Aktivt jobb eller null om inget finns</returns>
        JobProfile GetActiveJob();

        /// <summary>
        /// Kontrollera om användaren har arbetat idag
        /// </summary>
        /// <param name="jobProfileId">ID för jobbprofil</param>
        /// <returns>True om det finns pass för idag</returns>
        bool HasWorkedToday(int jobProfileId);

        /// <summary>
        /// Hämta dagens totala arbetstid
        /// </summary>
        /// <param name="jobProfileId">ID för jobbprofil</param>
        /// <returns>Antal timmar arbetade idag</returns>
        decimal GetTodaysHours(int jobProfileId);

        /// <summary>
        /// Hämta snabb-statistik för dashboard
        /// </summary>
        /// <param name="jobProfileId">ID för jobbprofil</param>
        /// <returns>Snabb översikt</returns>
        DashboardSummary GetDashboardSummary(int jobProfileId);
        #endregion

        #region FlexTime Methods
        /// <summary>
        /// Hämta aktuellt flex-saldo
        /// </summary>
        /// <param name="jobProfileId">ID för jobbprofil</param>
        /// <returns>Aktuellt totalt flex-saldo</returns>
        decimal GetCurrentFlexBalance(int jobProfileId);

        decimal GetTotalFlexBalanceExcludingCurrentMonth(int jobProfileId);

        /// <summary>
        /// Hämta flex-historik för rapporter
        /// </summary>
        /// <param name="jobProfileId">ID för jobbprofil</param>
        /// <param name="monthsBack">Antal månader bakåt (default 12)</param>
        /// <returns>Flex-historik sorterad nyaste först</returns>
        List<FlexTimeBalance> GetFlexTimeHistory(int jobProfileId, int monthsBack = 12);

        /// <summary>
        /// Uppdatera flex-saldo för aktuell månad
        /// (anropas när nya shifts läggs till/ändras)
        /// </summary>
        /// <param name="jobProfileId">ID för jobbprofil</param>
        void UpdateCurrentMonthFlexBalance(int jobProfileId);

        /// <summary>
        /// Hämta flex-status för dashboard
        /// </summary>
        /// <param name="jobProfileId">ID för jobbprofil</param>
        /// <returns>Flex-status för UI</returns>
        FlexStatus GetFlexStatus(int jobProfileId);
        #endregion
    }

    #region Existing Classes
    /// <summary>
    /// Månadens sammanfattande statistik
    /// </summary>
    public class MonthlyStats
    {
        public decimal TotalHours { get; set; }
        public decimal TotalObHours { get; set; }
        public int WorkDays { get; set; }
        public decimal RegularHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public int SickDays { get; set; }
        public decimal SickLeaveHours { get; set; }
        public int VacationDays { get; set; }
        public DateTime MonthStart { get; set; }
        public DateTime MonthEnd { get; set; }

        // VAB-fält
        public int VabDays { get; set; }
        public decimal VabHours { get; set; } 

        // NYA PROPERTIES FÖR FLEX
        public decimal ExpectedHours { get; set; }        // Förväntade timmar denna månad
        public decimal FlexDifference { get; set; }       // +/- för månaden
        public decimal CurrentFlexBalance { get; set; }   // Totalt saldo
    }

    /// <summary>
    /// Senaste aktivitet för dashboard
    /// </summary>
    public class RecentActivityItem
    {
        public string Icon { get; set; }
        public string TimeText { get; set; }
        public string Description { get; set; }
        public string Duration { get; set; }
        public DateTime ActivityDate { get; set; }
        public ShiftType ActivityType { get; set; }
    }

    /// <summary>
    /// Dashboard sammanfattning
    /// </summary>
    public class DashboardSummary
    {
        public bool HasWorkedToday { get; set; }
        public decimal TodaysHours { get; set; }
        public decimal WeeklyHours { get; set; }
        public decimal MonthlyHours { get; set; }
        public decimal MonthlyObHours { get; set; }
        public string NextScheduledShift { get; set; }
        public bool HasUpcomingVacation { get; set; }
        public bool IsCurrentlySick { get; set; }

        // Flex properties
        public decimal CurrentFlexBalance { get; set; }   // Aktuellt flex-saldo
        public string FlexBalanceText { get; set; }       // "+12.5h kompledighet"
        public string FlexStatusIcon { get; set; }        // 📈📉⚖️
    }
    #endregion

    #region Flex status
    /// <summary>
    /// Flex-status för dashboard
    /// </summary>
    public class FlexStatus
    {
        public decimal CurrentBalance { get; set; }       // Aktuellt saldo
        public decimal MonthlyDifference { get; set; }    // Denna månads +/-
        public string BalanceText { get; set; }           // "+12.5h kompledighet"
        public string MonthlyText { get; set; }           // "+2.5h denna månad"
        public string StatusIcon { get; set; }            // 📈📉⚖️
        public bool HasFlexTime => CurrentBalance != 0;
        public bool IsPositive => CurrentBalance > 0;
        public bool IsNegative => CurrentBalance < 0;
    }
    #endregion
}
