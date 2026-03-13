using MyWorkSalary.Helpers.Localization;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MyWorkSalary.Helpers.Calendar
{
    public static class CalendarGenerator
    {
        public static List<CalendarWeek> GenerateMonth(DateTime month)
        {
            var weeks = new List<CalendarWeek>();
            var today = DateTime.Today;

            var firstDayOfMonth = new DateTime(month.Year, month.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            int startDayOfWeek = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;

            int dayCounter = 1;

            while (dayCounter <= lastDayOfMonth.Day)
            {
                var week = new CalendarWeek();
                week.Days = new List<CalendarDay>();

                for (int i = 0; i < 7; i++)
                {
                    if (weeks.Count == 0 && i < startDayOfWeek)
                    {
                        week.Days.Add(new CalendarDay
                        {
                            Date = DateTime.MinValue,
                            IsCurrentMonth = false
                        });
                    }
                    else if (dayCounter <= lastDayOfMonth.Day)
                    {
                        var date = new DateTime(month.Year, month.Month, dayCounter);

                        week.Days.Add(new CalendarDay
                        {
                            Date = date,
                            IsToday = date.Date == today,
                            IsCurrentMonth = true
                        });

                        dayCounter++;
                    }
                    else
                    {
                        week.Days.Add(new CalendarDay
                        {
                            Date = DateTime.MinValue,
                            IsCurrentMonth = false
                        });
                    }
                }

                var firstDay = week.Days.FirstOrDefault(d => d.IsCurrentMonth);

                if (firstDay != null)
                    week.WeekNumber = ISOWeek.GetWeekOfYear(firstDay.Date);

                weeks.Add(week);
            }

            return weeks;
        }

        public static List<string> GetWeekDayNames()
        {
            return new List<string>
            {
                LocalizationHelper.Translate("Day_Mon_Short"),
                LocalizationHelper.Translate("Day_Tue_Short"),
                LocalizationHelper.Translate("Day_Wed_Short"),
                LocalizationHelper.Translate("Day_Thu_Short"),
                LocalizationHelper.Translate("Day_Fri_Short"),
                LocalizationHelper.Translate("Day_Sat_Short"),
                LocalizationHelper.Translate("Day_Sun_Short")
            };
        }
    }
}