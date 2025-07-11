using SQLite;
using MyWorkSalary.Models.Enums;

namespace MyWorkSalary.Models.Specialized
{
    public class OnCallShift
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int WorkShiftId { get; set; }  // FK till WorkShift

        // Grundläggande tider
        public TimeSpan StandbyStartTime { get; set; }    // 18:00
        public TimeSpan StandbyEndTime { get; set; }      // 08:00
        public decimal StandbyHours { get; set; }         // 14h
        public decimal ActiveHours { get; set; } = 0;    // 4.5h

        // Ersättning (för preliminär beräkning)
        public decimal OnCallRatePerHour { get; set; }    // 40 kr/tim

        // Anteckningar
        public string? Notes { get; set; }

    }
}
