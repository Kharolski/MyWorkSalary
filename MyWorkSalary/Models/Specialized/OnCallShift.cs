using SQLite;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;

namespace MyWorkSalary.Models.Specialized
{
    public class OnCallShift
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int WorkShiftId { get; set; }  // FK till WorkShift
        [Ignore]
        public WorkShift? WorkShift { get; set; }

        // Grundläggande tider
        public TimeSpan StandbyStartTime { get; set; }    // 18:00
        public TimeSpan StandbyEndTime { get; set; }      // 08:00
        public decimal StandbyHours { get; set; }         // 14h

        // Snapshots från JobProfile (så historik inte ändras om settings ändras senare)
        public OnCallStandbyPayType StandbyPayTypeSnapshot { get; set; }
        public decimal StandbyPayAmountSnapshot { get; set; }

        public OnCallActivePayMode ActivePayModeSnapshot { get; set; }
        public decimal ActiveHourlyRateSnapshot { get; set; } // om DefaultHourly: spara uträknad timlön vid tillfället

        // Anteckningar
        public string? Notes { get; set; }

    }
}
