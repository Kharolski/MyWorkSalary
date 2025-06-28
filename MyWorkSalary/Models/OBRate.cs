using System.ComponentModel.DataAnnotations;

namespace MyWorkSalary.Models
{
    public class OBRate
    {
        public int Id { get; set; }

        public int JobProfileId { get; set; }               // Kopplar till JobProfile

        [Required]
        public string Name { get; set; } = string.Empty;    // "OB vardag natt"

        public TimeSpan StartTime { get; set; }             // 22:00
        public TimeSpan EndTime { get; set; }               // 06:00

        public decimal RatePerHour { get; set; }            // 56.70

        public OBCategory Category { get; set; }

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
    }

    public enum OBCategory
    {
        Evening,           // Kväll (18-22)
        Night,             // Natt (22-06)
        Weekend,           // Veckoslut (lördag)
        WeekendExtra,      // Veckoslut extra (söndag)
        Holiday            // Helgdag/röd dag
    }
}
