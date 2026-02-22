using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Reports;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public partial class SalaryPageViewModel
    {
        #region Jour / On Call

        #region Properties

        public string OnCallTotalPayText =>
            CurrentStats == null
                ? ""
                : CurrencyHelper.FormatCurrency(CurrentStats.OnCallTotalPay, ActiveJob.CurrencyCode);

        public string OnCallStandbyPayText =>
            CurrentStats == null
                ? ""
                : CurrencyHelper.FormatCurrency(CurrentStats.OnCallPay, ActiveJob.CurrencyCode);

        public string ActivePayText => CurrencyHelper.FormatCurrency(ActivePay, ActiveJob.CurrencyCode);

        public decimal ActivePay { get; set; }

        public bool ShowOnCall => CurrentStats?.HasOnCall == true;
        public bool HasOnCall => CurrentStats?.HasOnCall == true;

        public string ShiftNote => CurrentStats?.OnCallDetails?.FirstOrDefault()?.ShiftNote ?? "";

        public bool HasShiftNote => !string.IsNullOrWhiteSpace(ShiftNote);

        private bool _isOnCallExpanded;
        public bool IsOnCallExpanded
        {
            get => _isOnCallExpanded;
            set
            {
                if (_isOnCallExpanded == value)
                    return;

                _isOnCallExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OnCallChevronIcon));
            }
        }

        public string OnCallChevronIcon => IsOnCallExpanded ? "▼" : "▶";

        #endregion

        #region Commands
        public ICommand ToggleOnCallCardCommand => new Command(() =>
        {
            IsOnCallExpanded = !IsOnCallExpanded;
        });

        public ICommand ToggleOnCallGroupCommand => new Command<OnCallDayGroup>(group =>
        {
            if (group == null)
                return;

            foreach (var g in _onCallGrouped)
            {
                if (!ReferenceEquals(g, group) && g.IsExpanded)
                    g.IsExpanded = false;
            }

            group.IsExpanded = !group.IsExpanded;

            OnPropertyChanged(nameof(OnCallGrouped));
        });

        #endregion

        #region Grouping
        private List<OnCallDayGroup> _onCallGrouped = new();
        public IReadOnlyList<OnCallDayGroup> OnCallGrouped => _onCallGrouped;

        private void RebuildOnCallGrouped()
        {
            _onCallGrouped = new List<OnCallDayGroup>();

            if (CurrentStats?.OnCallDetails == null ||
                CurrentStats.OnCallDetails.Count == 0)
            {
                OnPropertyChanged(nameof(OnCallGrouped));
                return;
            }

            var currency = string.IsNullOrWhiteSpace(ActiveJob?.CurrencyCode)
                ? "SEK"
                : ActiveJob.CurrencyCode;

            _onCallGrouped = CurrentStats.OnCallDetails
                .OrderBy(d => d.Date)
                .Select(d =>
                {
                    var start = d.StandbyStart;
                    var end = d.StandbyEnd;

                    var group = new OnCallDayGroup
                    {
                        Start = start,
                        End = end,
                        CurrencyCode = currency,

                        StandbyHours = d.StandbyHours,
                        StandbyPay = d.StandbyPay,

                        ActiveHours = d.ActiveHours,
                        ActivePay = d.Callouts.Sum(c => c.ActivePay),

                        ShiftNote = d.ShiftNote,

                        Details = d.Callouts
                            .OrderBy(c => c.Date)
                            .ThenBy(c => c.Start)
                            .Select(c =>
                            {
                                var cStart = c.Date.Date.Add(c.Start);
                                var cEnd = c.Date.Date.Add(c.End);

                                if (cEnd <= cStart)
                                    cEnd = cEnd.AddDays(1);

                                return new OnCallDetailRow
                                {
                                    Start = cStart,
                                    End = cEnd,
                                    Hours = c.Hours,
                                    Pay = c.ActivePay,
                                    Notes = c.Notes,
                                    CurrencyCode = currency
                                };
                            })
                            .ToList()
                    };

                    return group;
                })
                .ToList();

            OnPropertyChanged(nameof(OnCallGrouped));
        }

        #endregion

        #endregion
    }
}
