using MyWorkSalary.Models.Enums;

namespace MyWorkSalary.Models.Reports
{
    /// <summary>
    /// Representerar en sammanställning av löne- och arbetstidsdata
    /// för en viss period (t.ex. en månad).
    /// </summary>
    public class SalaryStats
    {
        #region Timmar
        /// <summary>
        /// Totalt antal arbetade timmar under perioden.
        /// </summary>
        public decimal TotalHours { get; set; }

        /// <summary>
        /// Förväntade timmar baserat på anställningsgrad/schema.
        /// </summary>
        public decimal ExpectedHours { get; set; }

        /// <summary>
        /// Totalt antal OB-timmar.
        /// </summary>
        public decimal TotalObHours { get; set; }

        /// <summary>
        /// Flexbalans (plus eller minus timmar).
        /// </summary>
        public decimal FlexBalance { get; set; }
        #endregion

        #region Dagar
        /// <summary>
        /// Antal sjukdagar under perioden.
        /// </summary>
        public int SickDays { get; set; }

        /// <summary>
        /// Antal semesterdagar under perioden.
        /// </summary>
        public int VacationDays { get; set; }

        /// <summary>
        /// Antal VAB-dagar under perioden.
        /// </summary>
        public int VabDays { get; set; }

        public decimal JourHours { get; set; }
        #endregion

        #region Lön
        /// <summary>
        /// Grundlön (fast månadslön eller timlön * arbetade timmar).
        /// </summary>
        public decimal BaseSalary { get; set; }

        /// <summary>
        /// OB-tillägg.
        /// </summary>
        public decimal ObPay { get; set; }

        /// <summary>
        /// Övertidsersättning.
        /// </summary>
        public decimal OvertimePay { get; set; }

        /// <summary>
        /// Andra tillägg (t.ex. bonus, helgersättning).
        /// </summary>
        public decimal ExtraPay { get; set; }
        #endregion

        #region Beräknat
        /// <summary>
        /// Summerad total lön inkl. alla tillägg.
        /// </summary>
        public decimal TotalSalary => BaseSalary + ObPay + OvertimePay + ExtraPay;
        #endregion

        #region Details
        public List<ObDetails> ObDetails { get; set; } = new();

        #endregion
    }

}
