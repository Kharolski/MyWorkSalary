using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Enums;

namespace MyWorkSalary.Models.Templates
{
    public class OBRateTemplate
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";

        public List<OBRateTemplateRule> Rules { get; set; } = new();
    }

    public class OBRateTemplateRule
    {
        public string Name { get; set; } = "";
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal RatePerHour { get; set; }
        public int Priority { get; set; }

        // dagar
        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }

        public bool Holidays { get; set; }
        public bool BigHolidays { get; set; }

        public OBCategory Category { get; set; }

        // För preview i UI
        public string PreviewText => $"{GetDaysText()} • {StartTime:hh\\:mm}–{EndTime:hh\\:mm}";

        // För preview valuta i UI
        public string CurrencyCode { get; set; } = "SEK";
        public string RateDisplayText => CurrencyHelper.FormatCurrency(RatePerHour, CurrencyCode);

        private string GetDaysText()
        {
            // Specialfall
            if (BigHolidays)
                return LocalizationHelper.Translate("DayGroup_BigHolidays");
            if (Holidays)
                return LocalizationHelper.Translate("DayGroup_Holidays");

            var days = new List<string>();

            if (Monday)
                days.Add(LocalizationHelper.Translate("Day_Mon_Short"));
            if (Tuesday)
                days.Add(LocalizationHelper.Translate("Day_Tue_Short"));
            if (Wednesday)
                days.Add(LocalizationHelper.Translate("Day_Wed_Short"));
            if (Thursday)
                days.Add(LocalizationHelper.Translate("Day_Thu_Short"));
            if (Friday)
                days.Add(LocalizationHelper.Translate("Day_Fri_Short"));
            if (Saturday)
                days.Add(LocalizationHelper.Translate("Day_Sat_Short"));
            if (Sunday)
                days.Add(LocalizationHelper.Translate("Day_Sun_Short"));

            return days.Count == 0
                ? LocalizationHelper.Translate("DayGroup_NoneSelected")
                : string.Join(", ", days);
        }
    }
}
