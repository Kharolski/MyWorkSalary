using SQLite;

namespace MyWorkSalary.Models.Specialized
{
    /// <summary>
    /// Definierar när dag, kväll och natt börjar för ett jobb
    /// </summary>
    public class ShiftTimeSettings
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int JobProfileId { get; set; } // Koppling till JobProfile

        // ====================
        // Vardagar
        // ====================
        public TimeSpan DayStart { get; set; } = new(06, 00, 00);      // dag börjar 06:00
        public TimeSpan EveningStart { get; set; } = new(18, 00, 00);  // kväll börjar 18:00
        public TimeSpan NightStart { get; set; } = new(22, 00, 00);    // natt börjar 22:00

        public bool EveningActive { get; set; } = true;   // true = OB gäller
        public bool NightActive { get; set; } = true;

        // ====================
        // Helger (kan ha egna tider om avtal kräver)
        // ====================
        public TimeSpan WeekendDayStart { get; set; } = new(06, 00, 00);
        public TimeSpan WeekendEveningStart { get; set; } = new(18, 00, 00);
        public TimeSpan WeekendNightStart { get; set; } = new(22, 00, 00);

        public bool WeekendEveningActive { get; set; } = true;
        public bool WeekendNightActive { get; set; } = true;

        // ====================
        // Metadata
        // ====================
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
    }
}
