using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.ViewModels;
using System.Collections.ObjectModel;

namespace MyWorkSalary.Models.Reports
{
    public class ObCategoryGroupRow : BaseViewModel
    {
        public OBCategory Category { get; set; }
        public OBDayType DayType { get; set; }

        public string DisplayName { get; set; } = "";

        public decimal TotalHours { get; set; }
        public decimal TotalPay { get; set; }

        // Dessa används av XAML
        public string HoursText => $"{TotalHours:0.##} {LocalizationHelper.Translate("Hours_Abbreviation")}";

        // Vi vill formatta med valuta → injicera CurrencyCode som string
        public string CurrencyCode { get; set; } = "";
        public string TotalPayText => TotalPay <= 0
            ? "—"
            : CurrencyHelper.FormatCurrency(TotalPay, string.IsNullOrWhiteSpace(CurrencyCode) ? "SEK" : CurrencyCode);

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (SetProperty(ref _isExpanded, value))
                    OnPropertyChanged(nameof(Chevron));
            }
        }

        public string Chevron => IsExpanded ? "▼" : "▶";

        public List<ObCategoryDetailRow> Details { get; set; } = new();
    }

    public class ObCategoryDetailRow
    {
        public string DateText { get; set; } = "";
        public string HoursText { get; set; } = "";
        public string PayText { get; set; } = "";
    }
}
