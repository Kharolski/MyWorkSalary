using SQLite;

namespace MyWorkSalary.Models.Specialized
{
    public class OnCallCallout
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int OnCallShiftId { get; set; }     // FK till OnCallShift

        public TimeSpan StartTime { get; set; }    // 21:30
        public TimeSpan EndTime { get; set; }      // 22:10

        public string? Notes { get; set; }
    }
}
