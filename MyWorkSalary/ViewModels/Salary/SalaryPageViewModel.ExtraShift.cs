using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Reports;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public partial class SalaryPageViewModel
    {
        #region Extra Shift (Salary UI)

        public bool ShowExtraPay => CurrentStats?.ExtraPay > 0;

        public string ExtraPayText =>
            CurrentStats == null
                ? "–"
                : FormatMoney(CurrentStats.ExtraPay);

        public bool HasExtraShifts => CurrentStats?.HasExtraShifts == true;

        private bool _isExtraExpanded;
        public bool IsExtraExpanded
        {
            get => _isExtraExpanded;
            set
            {
                if (_isExtraExpanded == value)
                    return;

                _isExtraExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ExtraChevronIcon));
            }
        }
        public string ExtraChevronIcon => IsExtraExpanded ? "▼" : "▶";

        public string TotalExtraHoursText =>
            CurrentStats == null
                ? ""
                : $"{CurrentStats.TotalExtraShiftHours:0.0}";

        // UI‑modell för raderna
        public class ExtraShiftRow
        {
            public string DateText { get; set; } = "";
            public string HoursText { get; set; } = "";
            public string PayText { get; set; } = "";
        }

        private List<ExtraShiftRow> _extraShiftRows = new();
        public IReadOnlyList<ExtraShiftRow> ExtraShiftRows => _extraShiftRows;

        private void RebuildExtraShiftRows()
        {
            _extraShiftRows = new List<ExtraShiftRow>();

            if (CurrentStats?.ExtraShiftDetails == null ||
                !CurrentStats.ExtraShiftDetails.Any())
            {
                OnPropertyChanged(nameof(ExtraShiftRows));
                return;
            }

            _extraShiftRows = CurrentStats.ExtraShiftDetails
                .OrderBy(x => x.Date)
                .Select(x => new ExtraShiftRow
                {
                    DateText = x.Date.ToString("dd-MM", AppCulture),
                    HoursText = $"{x.Hours:0.##} {LocalizationHelper.Translate("Hours_Abbreviation")}",
                    PayText = FormatMoney(x.ExtraPay)
                })
                .ToList();

            OnPropertyChanged(nameof(ExtraShiftRows));
        }

        public ICommand ToggleExtraCardCommand => new Command(() =>
        {
            IsExtraExpanded = !IsExtraExpanded;
        });

        #endregion
    }
}
