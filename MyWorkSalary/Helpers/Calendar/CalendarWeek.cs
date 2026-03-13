namespace MyWorkSalary.Helpers.Calendar
{
    public class CalendarWeek
    {
        public int WeekNumber { get; set; }
        public List<CalendarDay> Days { get; set; } = new();
    }
}
