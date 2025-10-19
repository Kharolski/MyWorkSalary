using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Enums;
using SQLite;
using System.ComponentModel.DataAnnotations;

namespace MyWorkSalary.Models.Specialized
{
    /// <summary>
    /// Specialiserade OB-regler för olika tider och dagar
    /// </summary>
    public class OBRate
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int JobProfileId { get; set; }               // Kopplar till JobProfile

        [Required]
        public string Name { get; set; } = string.Empty;    // "OB vardag natt"

        public TimeSpan StartTime { get; set; }             // 22:00
        public TimeSpan EndTime { get; set; }               // 06:00
        public decimal RatePerHour { get; set; }            // 56.70
        public OBCategory Category { get; set; }

        public string CurrencyCode { get; set; } = "SEK";

        // Vilka dagar gäller detta? (Flexibel approach)
        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }
        public bool Holidays { get; set; }                  // Röda dagar

        // Metadata
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        [Ignore] // så att SQLite inte försöker spara den
        public string RateDisplayText
        {
            get
            {
                var currency = CurrencyCode ?? "SEK"; // kommer alltid sättas i LoadOBRates()
                return CurrencyHelper.FormatCurrency(RatePerHour, currency);
            }
        }
    }
}
