using MyWorkSalary.Helpers.Localization;

namespace MyWorkSalary.Models.Reports
{
    public class OnCallDetailRow
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public string TimeRangeText => $"{Start:HH:mm}–{End:HH:mm}";

        public decimal Hours { get; set; }
        public string HoursText =>
            $"{Hours:0.##} {LocalizationHelper.Translate("Hours_Abbreviation")}";

        public decimal Pay { get; set; }
        public string CurrencyCode { get; set; } = "SEK";
        public string PayText =>
            CurrencyHelper.FormatCurrency(Pay, CurrencyCode);

        public string? Notes { get; set; }
        public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    }
}
