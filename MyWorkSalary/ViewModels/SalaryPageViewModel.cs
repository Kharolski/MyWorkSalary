using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Reports;
using MyWorkSalary.Services.Handlers;
using MyWorkSalary.Services.Interfaces;
 using System.Globalization;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public class SalaryPageViewModel : BaseViewModel
    {
        #region Private Fields
        private readonly IDashboardService _dashboardService;
        private readonly IJobProfileRepository _jobProfileRepository;
        private readonly SalaryStatsHandler _salaryHandler;

        private JobProfile _activeJob;
        private SalaryStats _currentStats;

        private DateTime _selectedMonth;
        private CultureInfo AppCulture => TranslationManager.Instance.CurrentCulture;

        // Att slippa anropa nya Command() varje gång
        private ICommand _prevMonthCommand;
        private ICommand _nextMonthCommand;

        private bool _isObExpanded;
        private bool _isBalanceExpanded;
        #endregion

        #region Public Properties

        public JobProfile ActiveJob
        {
            get => _activeJob;
            set
            {
                _activeJob = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasActiveJob));
                OnPropertyChanged(nameof(WelcomeText));

                if (_activeJob != null)
                {
                    RefreshStats();
                }
                else
                {
                    CurrentStats = null;
                }
            }
        }

        public bool HasActiveJob => ActiveJob != null;

        public string WelcomeText => HasActiveJob 
            ? $"{LocalizationHelper.Translate("ActiveJob")} - {ActiveJob.JobTitle}" 
            : LocalizationHelper.Translate("WelcomeMessage");

        public SalaryStats CurrentStats
        {
            get => _currentStats;
            private set
            {
                _currentStats = value;
                OnPropertyChanged();
                NotifyStatsBindingsChanged();
            }
        }

        public DateTime SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                // normalisera till första dagen i månaden
                var normalized = new DateTime(value.Year, value.Month, 1);

                if (_selectedMonth == normalized)
                    return;

                _selectedMonth = normalized;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentMonthYearText));
                OnPropertyChanged(nameof(CanGoNextMonth));
                OnPropertyChanged(nameof(CanGoPrevMonth));

                // badge för rätt månad
                OnPropertyChanged(nameof(IsCurrentMonth));
                OnPropertyChanged(nameof(CurrentMonthBadgeText));
                OnPropertyChanged(nameof(ShowCurrentMonthBadge));

                // info text om OB betalning 
                OnPropertyChanged(nameof(ObPeriodHintText));

                RefreshStats();
            }
        }

        public bool CanGoNextMonth
        {
            get
            {
                // Visas bara upp till 5 framtida månader 
                var maxMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(5);
                return SelectedMonth < maxMonth;
            }
        }
        public bool CanGoPrevMonth
        {
            get
            {
                // valfritt: begränsa bakåt t.ex. 24 månader
                var minMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-24);
                return SelectedMonth > minMonth;
            }
        }

        #endregion

        // ==== Formaterade properties för XAML ====
        #region UI Text Properties
        public bool IsCurrentMonth
        {
            get
            {
                var now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                return SelectedMonth == now;
            }
        }
        public string CurrentMonthBadgeText => IsCurrentMonth ? LocalizationHelper.Translate("Common_Current") : "";
        public bool ShowCurrentMonthBadge => IsCurrentMonth;

        public string MonthlySalaryText => CurrentStats == null ? "–" : FormatMoney(CurrentStats.NetSalary);
        public string GrossSalaryText => CurrentStats == null ? "–" : FormatMoney(CurrentStats.GrossSalary);
        public string CurrentMonthYearText => SelectedMonth.ToString("MMMM yyyy", AppCulture);
        public string HoursSummaryText
        {
            get
            {
                if (ActiveJob == null || CurrentStats == null)
                    return "";

                if (ActiveJob.EmploymentType == EmploymentType.Permanent)
                {
                    var diff = CurrentStats.TotalHours - CurrentStats.ExpectedHours;

                    if (diff == 0)
                        return LocalizationHelper.Translate("Salary_Hours_Balanced");

                    var absDiff = Math.Abs(diff);

                    return diff > 0
                        ? string.Format(LocalizationHelper.Translate("Salary_Hours_Surplus"), absDiff)
                        : string.Format(LocalizationHelper.Translate("Salary_Hours_Deficit"), absDiff);
                }
                else // Timanställd
                {
                    return string.Format(LocalizationHelper.Translate("Salary_TotalHours"),
                        CurrentStats.TotalHours, LocalizationHelper.Translate("Hours_Abbreviation"));
                }
            }
        }
        public decimal HoursSummaryColor
        {
            get
            {
                if (ActiveJob == null || CurrentStats == null)
                    return 0;
                if (ActiveJob.EmploymentType == EmploymentType.Permanent)
                {
                    // Returnera skillnaden för färgberäkning
                    return CurrentStats.TotalHours - CurrentStats.ExpectedHours;
                }
                else
                {
                    // För timanställda, använd bara totala timmar
                    return CurrentStats.TotalHours;
                }
            }
        }

        public string BaseSalaryText
        {
            get
            {
                if (ActiveJob == null)
                    return LocalizationHelper.Translate("Salary_BaseSalary_NotSet");
                if (ActiveJob.EmploymentType == EmploymentType.Permanent)
                {
                    return ActiveJob.MonthlySalary.HasValue
                        ? FormatMoney(ActiveJob.MonthlySalary.Value)
                        : LocalizationHelper.Translate("Salary_NotSet");
                }

                // Timanställd
                return ActiveJob.HourlyRate.HasValue
                    ? FormatRate(ActiveJob.HourlyRate.Value)
                    : LocalizationHelper.Translate("Salary_NotSet");
            }
        }
        public bool ShowVacationPay => ActiveJob?.EmploymentType != EmploymentType.Permanent;
        public string VacationPayText => CurrentStats == null ? "–" : FormatMoney(CurrentStats.VacationPay);

        public string TotalHoursText
        {
            get
            {
                if (CurrentStats == null)
                    return "–";

                // Timanställd
                return string.Format(
                    LocalizationHelper.Translate("Salary_TotalHours"),
                    CurrentStats.TotalHours,
                    LocalizationHelper.Translate("Hours_Abbreviation")
                );
            }
        }

        public string TaxText
        {
            get
            {
                if (ActiveJob == null || CurrentStats == null)
                    return "–";

                // Om user inte vill räkna skatt (t.ex. 0)
                if (CurrentStats.TaxRate <= 0)
                    return "–";

                return $"-{CurrencyHelper.FormatCurrency(CurrentStats.TaxAmount, JobCurrency)}";
            }
        }

        public string ObPeriodHintText
        {
            get
            {
                // du visar alltid en "utbetalningsmånad" i Salary-sidan
                var workMonth = SelectedMonth.AddMonths(-1);

                // Ex: "OB avser arbete december 2025"
                return string.Format(
                    LocalizationHelper.Translate("OB_PeriodHint"),
                    workMonth.ToString("MMMM yyyy", AppCulture)
                );
            }
        }
        public string TotalObHoursText => CurrentStats == null ? "" : $"{CurrentStats.TotalObHours:F1}";
        public Color ObHoursColor => (CurrentStats?.TotalObHours ?? 0) > 0 ? Colors.Green : Colors.Gray;
        public string ObPayText
        {
            get
            {
                if (CurrentStats == null)
                    return FormatMoney(0m);

                if (!CurrentStats.HasObRulesConfigured)
                    return "—";

                return FormatMoney(CurrentStats.ObPay);
            }
        }
        public bool ShowObStatus => !string.IsNullOrWhiteSpace(ObStatusText);
        public string ObStatusText
        {
            get
            {
                if (CurrentStats == null)
                    return "";

                if (!CurrentStats.HasObRulesConfigured)
                    return LocalizationHelper.Translate("OB_NotConfigured");

                if (CurrentStats.UsedObFallback && CurrentStats.ObPay <= 0)
                    return LocalizationHelper.Translate("OB_UsingFallback");

                return "";
            }
        }
        public Color ObStatusColor
        {
            get
            {
                if (CurrentStats == null)
                    return Color.FromArgb("#9E9E9E");   // grå
                if (!CurrentStats.HasObRulesConfigured)
                    return Color.FromArgb("#E53935"); // röd
                if (CurrentStats.UsedObFallback)
                    return Color.FromArgb("#FB8C00"); // orange
                return Color.FromArgb("#43A047"); // grön
            }
        }
        public bool ShowObCard
        {
            get
            {
                if (CurrentStats == null)
                    return false;

                // Visa bara om OB finns eller OB-regler är aktiva
                return CurrentStats.TotalObHours > 0
                       || CurrentStats.ObPay > 0;
            }
        }


        // Kort 4
        public bool IsPermanent => ActiveJob?.EmploymentType == EmploymentType.Permanent;
        public bool ShowTimeBank => IsPermanent;
        public string TimeBankText
        {
            get
            {
                if (!IsPermanent || ActiveJob == null)
                    return "";

                // Ex: "+12.5 h" eller "-3.0 h"
                var sign = ActiveJob.TimeBankHours > 0 ? "+" : "";
                return $"{sign}{ActiveJob.TimeBankHours:0.0} {LocalizationHelper.Translate("Hours_Abbreviation")}";
            }
        }
        public decimal TimeBankColorValue => IsPermanent && ActiveJob != null
                ? ActiveJob.TimeBankHours
                : 0m;
        public string ExpectedHoursText
        {
            get
            {
                if (!IsPermanent || CurrentStats == null)
                    return "-";

                return $"{CurrentStats.ExpectedHours:0.0} h";
            }
        }
        public bool ShowBalanceSeparator => ShowTimeBank || IsPermanent;

        public string SickDaysText => CurrentStats == null ? "" : $"{CurrentStats.SickDays}";

        public string VacationDaysText => CurrentStats == null ? "" : $"{CurrentStats.VacationDays}";

        // expand/collapse
        public bool IsObExpanded
        {
            get => _isObExpanded;
            set
            {
                if (_isObExpanded != value)
                {
                    _isObExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ObCardChevronIcon));
                }
            }
        }
        public string ObCardChevronIcon => IsObExpanded ? "▼" : "▶";

        // expand/collapse – Frånvaro & balans
        public bool IsBalanceExpanded
        {
            get => _isBalanceExpanded;
            set
            {
                if (_isBalanceExpanded != value)
                {
                    _isBalanceExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BalanceChevronIcon));
                }
            }
        }

        public string BalanceChevronIcon => IsBalanceExpanded ? "▼" : "▶";
        #endregion

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

            // viktig: använd ActiveJobs valuta
            var currency = string.IsNullOrWhiteSpace(ActiveJob.CurrencyCode) ? "SEK" : ActiveJob.CurrencyCode;

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

                    CurrencyCode = currency,    // viktigt för TotalPayText

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

            // Trigga refresh för Chevron/IsVisible
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

            // Visa bara tid när det är Evening/Night Ex: "Natt • Storhelg"
            return cat switch
            {
                OBCategory.Evening => $"{tEvening} • {dayText}",
                OBCategory.Night => $"{tNight} • {dayText}",
                _ => dayText    // visar bara: “Helgdag” (utan “Dag • …”)
            };
            
        }
        #endregion

        #region Extra Shift
        public bool ShowExtraPay => CurrentStats?.ExtraPay > 0;
        public string ExtraPayText => CurrentStats == null ? "–" : FormatMoney(CurrentStats.ExtraPay);
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
        public string TotalExtraHoursText => CurrentStats == null ? "" : $"{CurrentStats.TotalExtraShiftHours:0.0}";

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

            if (CurrentStats?.ExtraShiftDetails == null || !CurrentStats.ExtraShiftDetails.Any())
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

        #region Jour / On Call
        #region Properties
        public string OnCallTotalPayText =>
                CurrentStats == null ? "" :
                CurrencyHelper.FormatCurrency(CurrentStats.OnCallTotalPay, ActiveJob.CurrencyCode);
        public string OnCallStandbyPayText =>
                CurrentStats == null ? "" :
                CurrencyHelper.FormatCurrency(CurrentStats.OnCallPay, ActiveJob.CurrencyCode);

        public string ActivePayText => CurrencyHelper.FormatCurrency(ActivePay, ActiveJob.CurrencyCode);
        public decimal ActivePay { get; set; }
        public bool ShowOnCall => CurrentStats?.HasOnCall == true;

        // Visning
        public bool HasOnCall => CurrentStats?.HasOnCall == true;
        public string ShiftNote => CurrentStats?.OnCallDetails?.FirstOrDefault()?.ShiftNote ?? "";
        public bool HasShiftNote => !string.IsNullOrWhiteSpace(ShiftNote);


        // Expander
        private bool _isOnCallExpanded;
        public bool IsOnCallExpanded
        {
            get => _isOnCallExpanded;
            set { _isOnCallExpanded = value; OnPropertyChanged(); OnPropertyChanged(nameof(OnCallChevronIcon)); }
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

            // Stäng andra grupper
            foreach (var g in _onCallGrouped)
                if (!ReferenceEquals(g, group) && g.IsExpanded)
                    g.IsExpanded = false;

            group.IsExpanded = !group.IsExpanded;

            OnPropertyChanged(nameof(OnCallGrouped));
        });

        #endregion

        private List<OnCallDayGroup> _onCallGrouped = new(); 
        public IReadOnlyList<OnCallDayGroup> OnCallGrouped => _onCallGrouped;
        private void RebuildOnCallGrouped()
        {
            _onCallGrouped = new List<OnCallDayGroup>();

            if (CurrentStats?.OnCallDetails == null || CurrentStats.OnCallDetails.Count == 0)
            {
                OnPropertyChanged(nameof(OnCallGrouped));
                return;
            }

            var currency = string.IsNullOrWhiteSpace(ActiveJob?.CurrencyCode)
                ? "SEK"
                : ActiveJob.CurrencyCode;

            // === Loopar per jourpass ===
            _onCallGrouped = CurrentStats.OnCallDetails
                .OrderBy(d => d.Date)
                .Select(d =>
                {
                    // Bygg riktiga start/slut för jourpasset
                    var start = d.StandbyStart; 
                    var end = d.StandbyEnd;

                    var group = new OnCallDayGroup
                    {
                        Start = start,
                        End = end,
                        CurrencyCode = currency,

                        // Standby
                        StandbyHours = d.StandbyHours,
                        StandbyPay = d.StandbyPay,

                        // Aktiv
                        ActiveHours = d.ActiveHours,
                        ActivePay = d.Callouts.Sum(c => c.ActivePay),

                        // Notes
                        ShiftNote = d.ShiftNote,

                        // Callouts
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

        // domänmodeller för visning av jour/OnCall
        public class OnCallDayGroup : BaseViewModel
        {
            // === Datumspann för jourpasset ===
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string DateSpanText => $"{Start:dd'/'MM} – {End:dd'/'MM}";

            // === Standby ===
            public decimal StandbyHours { get; set; }
            public string StandbyHoursText => $"{StandbyHours:0.##} {LocalizationHelper.Translate("Hours_Abbreviation")}";

            public decimal StandbyPay { get; set; }
            public string StandbyPayText => CurrencyHelper.FormatCurrency(StandbyPay, CurrencyCode);

            // === Aktiv tid ===
            public decimal ActiveHours { get; set; }
            public string ActiveHoursText => $"{ActiveHours:0.##} {LocalizationHelper.Translate("Hours_Abbreviation")}";

            public decimal ActivePay { get; set; }
            public string ActivePayText => CurrencyHelper.FormatCurrency(ActivePay, CurrencyCode);

            // === Callouts ===
            public List<OnCallDetailRow> Details { get; set; } = new();
            public string CurrencyCode { get; set; }

            // === Notes för jourpasset ===
            public string? ShiftNote { get; set; }
            public bool HasShiftNote => !string.IsNullOrWhiteSpace(ShiftNote);

            // === Expand/Collapse ===
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

        public class OnCallDetailRow
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }

            public string TimeRangeText => $"{Start:HH:mm}–{End:HH:mm}";

            public decimal Hours { get; set; }
            public string HoursText => $"{Hours:0.##} {LocalizationHelper.Translate("Hours_Abbreviation")}";
            public decimal Pay { get; set; }
            public string CurrencyCode { get; set; }
            public string PayText => CurrencyHelper.FormatCurrency(Pay, CurrencyCode);

            public string? Notes { get; set; }
            public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
        }

        #endregion

        #region Formatting

        private string JobCurrency => ActiveJob?.CurrencyCode ?? "SEK";

        private string FormatMoney(decimal amount)
        {
            return CurrencyHelper.FormatCurrency(amount, JobCurrency);
        }

        private string FormatRate(decimal amountPerHour)
        {
            // Ex: "150,00 kr/tim" eller "$20.00 /h" (du kan justera suffix senare)
            return $"{CurrencyHelper.FormatCurrency(amountPerHour, JobCurrency)}/tim";
        }

        #endregion

        #region Commands
        // Command för att toggla
        public ICommand ToggleObCardCommand => new Command(() =>
        {
            IsObExpanded = !IsObExpanded;
        });

        public ICommand ToggleBalanceCommand => new Command(() =>
        {
            IsBalanceExpanded = !IsBalanceExpanded;
        });

        public ICommand PrevMonthCommand => _prevMonthCommand ??= new Command(() =>
        {
            if (!CanGoPrevMonth)
                return;

            SelectedMonth = SelectedMonth.AddMonths(-1);
        });

        public ICommand NextMonthCommand => _nextMonthCommand ??= new Command(() =>
        {
            if (!CanGoNextMonth)
                return;

            SelectedMonth = SelectedMonth.AddMonths(1);
        });

        #endregion

        #region Constructor
        public SalaryPageViewModel(IDashboardService dashboardService,
            IJobProfileRepository jobProfileRepository,
            SalaryStatsHandler salaryStatsHandler)
        {
            _dashboardService = dashboardService;
            _jobProfileRepository = jobProfileRepository;
            _salaryHandler = salaryStatsHandler;

            _selectedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            _ = LoadData();
        }
        #endregion

        #region Private Methods
        public async Task LoadData()
        {
            ActiveJob = _jobProfileRepository.GetActiveJob();
            if (!HasActiveJob)
                return;

            RefreshStats();

        }

        private void NotifyStatsBindingsChanged()
        {
            OnPropertyChanged(nameof(MonthlySalaryText));
            OnPropertyChanged(nameof(CurrentMonthYearText));
            OnPropertyChanged(nameof(HoursSummaryText));
            OnPropertyChanged(nameof(HoursSummaryColor));
            OnPropertyChanged(nameof(BaseSalaryText));
            OnPropertyChanged(nameof(TotalHoursText));

            OnPropertyChanged(nameof(TaxText));

            // KORT 2 Extra shift
            OnPropertyChanged(nameof(ShowExtraPay));
            OnPropertyChanged(nameof(ExtraPayText));
            OnPropertyChanged(nameof(HasExtraShifts));
            OnPropertyChanged(nameof(TotalExtraHoursText));

            // KORT - Jour
            OnPropertyChanged(nameof(ActivePayText));
            OnPropertyChanged(nameof(ShowOnCall));
            OnPropertyChanged(nameof(OnCallTotalPayText));
            OnPropertyChanged(nameof(HasOnCall));
            OnPropertyChanged(nameof(OnCallChevronIcon));
            RebuildOnCallGrouped();

            // KORT 3 - OB
            OnPropertyChanged(nameof(TotalObHoursText));
            OnPropertyChanged(nameof(ObDetails));
            OnPropertyChanged(nameof(ObPayText));
            OnPropertyChanged(nameof(ObHoursColor));
            OnPropertyChanged(nameof(ShowObStatus));
            OnPropertyChanged(nameof(ObPeriodHintText));
            OnPropertyChanged(nameof(ObStatusText));
            OnPropertyChanged(nameof(ObStatusColor));
            OnPropertyChanged(nameof(ShowObCard));

            // KORT 4 
            OnPropertyChanged(nameof(IsPermanent));
            OnPropertyChanged(nameof(ShowTimeBank));
            OnPropertyChanged(nameof(TimeBankText));
            OnPropertyChanged(nameof(TimeBankColorValue));
            OnPropertyChanged(nameof(ExpectedHoursText));
            OnPropertyChanged(nameof(ShowBalanceSeparator));

            RebuildObGrouped();

            OnPropertyChanged(nameof(SickDaysText));
            OnPropertyChanged(nameof(ShowVacationPay));
            OnPropertyChanged(nameof(VacationPayText));
            OnPropertyChanged(nameof(VacationDaysText));
        }

        private void RefreshStats()
        {
            if (ActiveJob == null)
            {
                CurrentStats = null;
                RebuildObGrouped();
                return;
            }

            CurrentStats = _salaryHandler.CalculateMonthlyStats(ActiveJob.Id, SelectedMonth);

            RebuildExtraShiftRows();

            // bygg grupperna direkt efter att CurrentStats är klar
            RebuildObGrouped();
        }
        public void ResetToCurrentMonth()
        {
            var now = DateTime.Now;
            SelectedMonth = new DateTime(now.Year, now.Month, 1);
        }

        #endregion
    }
}
