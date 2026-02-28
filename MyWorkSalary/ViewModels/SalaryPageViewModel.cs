using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Reports;
using MyWorkSalary.Services.Handlers;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Services.Premium;
using System.Globalization;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public partial class SalaryPageViewModel : BaseViewModel
    {
        #region Private Fields
        private readonly IDashboardService _dashboardService;
        private readonly IJobProfileRepository _jobProfileRepository;
        private readonly SalaryStatsHandler _salaryHandler;
        private readonly AdService _adService;

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
        // Flyttad till partial-fil: ViewModels/Salary/SalaryPageViewModel.OB.cs
        #endregion

        #region Extra Shift (Salary UI)
        // Flyttad till partial-fil: ViewModels/Salary/SalaryPageViewModel.ExtraShift.cs
        #endregion

        #region Jour / OnCall
        // Flyttad till partial-fil: ViewModels/Salary/SalaryPageViewModel.OnCall.cs
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
            SalaryStatsHandler salaryStatsHandler,
            AdService adService)
        {
            _dashboardService = dashboardService;
            _jobProfileRepository = jobProfileRepository;
            _salaryHandler = salaryStatsHandler;
            _adService = adService;

            _selectedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            IsBusy = true;

            _ = LoadData();
        }
        #endregion

        #region Private Methods
        public async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;
                
                // Ladda data i bakgrunden för snabbare UI
                await Task.Run(() =>
                {
                    try
                    {
                        // Ladda aktivt jobb
                        var activeJob = _jobProfileRepository.GetActiveJob();
                        
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ActiveJob = activeJob;
                        });

                        if (!HasActiveJob)
                            return;

                        // Ladda statistik
                        RefreshStats();
                    }
                    catch (Exception dataEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"🚨 SalaryPage data loading error: {dataEx}");
                        throw; // Kasta vidare för att hanteras i yttre catch
                    }
                });
                
                // Visa banner efter att data har laddats (om inte premium)
                try
                {
                    _adService.ShowBanner();
                }
                catch (Exception adEx)
                {
                    System.Diagnostics.Debug.WriteLine($"🚨 SalaryPage ad service error: {adEx}");
                    // Fortsätt även om banner misslyckas
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🚨 SalaryPage LoadDataAsync Error: {ex}");
                System.Diagnostics.Debug.WriteLine($"🚨 Stack Trace: {ex.StackTrace}");
                throw; // Kasta vidare för att hanteras i SalaryPage
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadData()
        {
            await LoadDataAsync();
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
