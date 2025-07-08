using SQLite;
using System.ComponentModel.DataAnnotations;

namespace MyWorkSalary.Models.Core
{
    public class FlexTimeBalance
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Required]
        public int JobProfileId { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public int Month { get; set; }  // 1-12

        #region Monthly Data
        /// <summary>
        /// Förväntade timmar för denna månad (från JobProfile.ExpectedHoursPerMonth)
        /// </summary>
        public decimal ExpectedHours { get; set; }

        /// <summary>
        /// Faktiskt arbetade timmar denna månad
        /// </summary>
        public decimal ActualHours { get; set; }

        /// <summary>
        /// Skillnad för denna månad (ActualHours - ExpectedHours)
        /// Positivt = övertid/kompledighet, Negativt = skuld
        /// </summary>
        public decimal MonthlyDifference { get; set; }
        #endregion

        #region Running Balance
        /// <summary>
        /// Ackumulerat saldo efter denna månad
        /// Summan av alla MonthlyDifference fram till denna månad
        /// </summary>
        public decimal RunningBalance { get; set; }
        #endregion

        #region Metadata
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// Anteckningar för månaden
        /// </summary>
        public string? Notes { get; set; }
        #endregion

        #region Computed Properties (Ignore för SQLite)
        [Ignore]
        public bool IsOvertime => MonthlyDifference > 0;

        [Ignore]
        public bool IsUndertime => MonthlyDifference < 0;

        [Ignore]
        public bool IsBalanced => MonthlyDifference == 0;

        [Ignore]
        public string MonthName => new DateTime(Year, Month, 1)
            .ToString("MMMM yyyy", new System.Globalization.CultureInfo("sv-SE"));

        [Ignore]
        public string MonthlyDifferenceText => MonthlyDifference switch
        {
            > 0 => $"+{MonthlyDifference:F1} tim kompledighet",
            < 0 => $"{MonthlyDifference:F1} tim skuld",
            _ => "Balanserat"
        };

        [Ignore]
        public string RunningBalanceText => RunningBalance switch
        {
            > 0 => $"+{RunningBalance:F1} tim kompledighet",
            < 0 => $"{RunningBalance:F1} tim skuld",
            _ => "Balanserat"
        };

        [Ignore]
        public string StatusIcon => MonthlyDifference switch
        {
            > 0 => "📈", // Övertid
            < 0 => "📉", // Skuld
            _ => "⚖️"    // Balanserat
        };
        #endregion

        #region Helper Methods
        /// <summary>
        /// Skapa unik nyckel för år/månad kombination
        /// </summary>
        [Ignore]
        public string YearMonthKey => $"{Year:0000}-{Month:00}";

        /// <summary>
        /// Kontrollera om detta är aktuell månad
        /// </summary>
        [Ignore]
        public bool IsCurrentMonth =>
            Year == DateTime.Now.Year && Month == DateTime.Now.Month;

        /// <summary>
        /// Kontrollera om detta är framtida månad
        /// </summary>
        [Ignore]
        public bool IsFutureMonth =>
            new DateTime(Year, Month, 1) > new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        #endregion
    }
}
