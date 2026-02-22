using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Models.Reports
{
    public class OnCallDayGroup : BaseViewModel
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string DateSpanText => $"{Start:dd'/'MM} – {End:dd'/'MM}";

        public decimal StandbyHours { get; set; }
        public string StandbyHoursText =>
            $"{StandbyHours:0.##} {LocalizationHelper.Translate("Hours_Abbreviation")}";

        public decimal StandbyPay { get; set; }
        public string StandbyPayText =>
            CurrencyHelper.FormatCurrency(StandbyPay, CurrencyCode);

        public decimal ActiveHours { get; set; }
        public string ActiveHoursText =>
            $"{ActiveHours:0.##} {LocalizationHelper.Translate("Hours_Abbreviation")}";

        public decimal ActivePay { get; set; }
        public string ActivePayText =>
            CurrencyHelper.FormatCurrency(ActivePay, CurrencyCode);

        public List<OnCallDetailRow> Details { get; set; } = new();
        public string CurrencyCode { get; set; } = "SEK";

        public string? ShiftNote { get; set; }
        public bool HasShiftNote => !string.IsNullOrWhiteSpace(ShiftNote);

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
                OnPropertyChanged(nameof(Chevron));
            }
        }

        public string Chevron => IsExpanded ? "▼" : "▶";
    }
}
