namespace MyWorkSalary.Models.Enums
{
    public enum OBCategory
    {
        Day,                // Dag (06-19)
        Evening,            // Kväll (19-22)
        Night               // Natt (22-06)
    }

    public enum OBDayType
    {
        Weekday = 0,
        Weekend = 1,
        Holiday = 2,
        BigHoliday = 3
    }
}
