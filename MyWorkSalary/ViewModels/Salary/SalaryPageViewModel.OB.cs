using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Reports;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public partial class SalaryPageViewModel
    {
        #region OB Grouped UI

        private List<ObCategoryGroupRow> _obGrouped = new();
        public IReadOnlyList<ObCategoryGroupRow> ObGrouped => _obGrouped;

        private void RebuildObGrouped()
        {
            _obGrouped = new List<ObCategoryGroupRow>();

            if (CurrentStats == null || !CurrentStats.HasObRulesConfigured)
            {
                OnPropertyChanged(nameof(ObGrouped));
                return;
            }

            var currency = string.IsNullOrWhiteSpace(ActiveJob.CurrencyCode)
                ? "SEK"
                : ActiveJob.CurrencyCode;

            var rows = CurrentStats.ObDetails ?? new List<ObDetails>();

            _obGrouped = rows
                .Where(x => x.Hours > 0)
                .GroupBy(x => new { x.Category, x.DayType })
                .OrderByDescending(g => g.Sum(x => x.Pay))
                .Select(g => new ObCategoryGroupRow
                {
                    Category = g.Key.Category,
                    DayType = g.Key.DayType,

                    DisplayName = BuildObDisplayName(g.Key.Category, g.Key.DayType),

                    CurrencyCode = currency,

                    TotalHours = Math.Round(g.Sum(x => x.Hours), 2),
                    TotalPay = Math.Round(g.Sum(x => x.Pay), 2),

                    Details = g
                        .OrderBy(x => x.Date)
                        .Select(d => new ObCategoryDetailRow
                        {
                            DateText = d.Date.ToString("dd-MM", AppCulture),
                            HoursText = $"{d.Hours:0.##} {LocalizationHelper.Translate("Hours_Abbreviation")}",
                            PayText = CurrencyHelper.FormatCurrency(d.Pay, currency)
                        })
                        .ToList()
                })
                .ToList();

            OnPropertyChanged(nameof(ObGrouped));
        }

        public ICommand ToggleObGroupCommand => new Command<ObCategoryGroupRow>(row =>
        {
            if (row == null)
                return;

            foreach (var r in _obGrouped)
            {
                if (!ReferenceEquals(r, row) && r.IsExpanded)
                    r.IsExpanded = false;
            }

            row.IsExpanded = !row.IsExpanded;

            OnPropertyChanged(nameof(ObGrouped));
        });

        #endregion

        #region OB Display Helpers
        private string BuildObDisplayName(OBCategory cat, OBDayType dayType)
        {
            var tDay = LocalizationHelper.Translate("OBTime_Day");
            var tEvening = LocalizationHelper.Translate("OBTime_Evening");
            var tNight = LocalizationHelper.Translate("OBTime_Night");

            var dWeekday = LocalizationHelper.Translate("OBDay_Weekday");
            var dWeekend = LocalizationHelper.Translate("OBDay_Weekend");
            var dHoliday = LocalizationHelper.Translate("OBDay_Holiday");
            var dBigHoliday = LocalizationHelper.Translate("OBDay_BigHoliday");

            var dayText = dayType switch
            {
                OBDayType.BigHoliday => dBigHoliday,
                OBDayType.Holiday => dHoliday,
                OBDayType.Weekend => dWeekend,
                _ => dWeekday
            };

            return cat switch
            {
                OBCategory.Evening => $"{tEvening} • {dayText}",
                OBCategory.Night => $"{tNight} • {dayText}",
                _ => dayText
            };
        }
        #endregion

        
    }
}
