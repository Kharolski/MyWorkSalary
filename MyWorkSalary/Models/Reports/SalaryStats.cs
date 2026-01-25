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
        public decimal TotalHours { get; set; }
        public decimal ExpectedHours { get; set; }
        public decimal TotalObHours { get; set; }
        public decimal FlexBalance { get; set; }
        public decimal JourHours { get; set; }
        #endregion

        #region Frånvaro
        public int SickDays { get; set; }
        public int VacationDays { get; set; }
        public int VabDays { get; set; }
        #endregion

        #region Lön – BRUTTO (före skatt)
        public decimal BaseSalary { get; set; }
        public decimal ObPay { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal VacationPay { get; set; } // semesterersättning (timanställd)
        public decimal ExtraPay { get; set; }

        /// <summary>
        /// Total bruttolön (före skatt)
        /// </summary>
        public decimal GrossSalary => BaseSalary + ObPay + VacationPay + OvertimePay + ExtraPay;
        #endregion

        #region Skatt
        /// <summary>
        /// Skatteavdrag
        /// </summary>
        public decimal TaxAmount { get; set; }

        /// <summary>
        /// Skattesats (t.ex. 0.33)
        /// </summary>
        public decimal TaxRate { get; set; }
        #endregion

        #region Lön – NETTO (utbetalt)
        /// <summary>
        /// Nettolön efter skatt
        /// </summary>
        public decimal NetSalary => GrossSalary - TaxAmount;
        #endregion

        #region OB-detaljer
        public List<ObDetails> ObDetails { get; set; } = new();
        public bool UsedObFallback { get; set; } // true när vi räknar "live" istället för OBEvents
        public string? ObInfoNote { get; set; } // valfri text till UI
        #endregion
    }

}
