using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using SQLite;

public class VABLeave
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int WorkShiftId { get; set; }

    public VABType VABType { get; set; }

    // Arbetstider
    public TimeSpan? WorkedStartTime { get; set; }
    public TimeSpan? WorkedEndTime { get; set; }
    public TimeSpan? ScheduledStartTime { get; set; }
    public TimeSpan? ScheduledEndTime { get; set; }

    // Timmar
    public decimal ScheduledHours { get; set; }
    public decimal WorkedHours { get; set; } = 0;
    public decimal VABHours { get; set; }               // Förlorade timmar (positiv)

    // Frysta värden (timanställda)
    public decimal? WeeklyHoursUsed { get; set; }
    public decimal? HourlyRateUsed { get; set; }
    public decimal? WeeklyEarningsUsed { get; set; }

    // Ekonomi (NEGATIVA VÄRDEN för avdrag)
    public decimal WorkedPay { get; set; } = 0;         // Positiv - lön för jobbade timmar
    public decimal VABDeduction { get; set; }           // NEGATIV - avdrag från lön

    // Metadata
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? ModifiedDate { get; set; }
    public string? Notes { get; set; }

}
