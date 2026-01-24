using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Reports;
using MyWorkSalary.Services.Handlers;
using MyWorkSalary.Services.Interfaces;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public class SalaryPageViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly IDashboardService _dashboardService;
        private readonly IJobProfileRepository _jobProfileRepository;
        private readonly SalaryStatsHandler _salaryHandler;

        private JobProfile _activeJob;
        private SalaryStats _currentStats;

        private DateTime _selectedMonth;

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

        public string WelcomeText => HasActiveJob ? $"Aktivt jobb - {ActiveJob.JobTitle}" : "Välkommen till din löneapp!";

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
        public string MonthlySalaryText => CurrentStats == null ? "–" : $"{CurrentStats.NetSalary:N0} kr";
        public string GrossSalaryText => CurrentStats == null ? "–" : $"{CurrentStats.GrossSalary:N0} kr";
        public string CurrentMonthYearText => SelectedMonth.ToString("MMMM yyyy", new CultureInfo("sv-SE"));
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
                        return "I balans mot schema";

                    var absDiff = Math.Abs(diff);

                    return diff > 0
                        ? $"Överskott: +{absDiff:F1} t"
                        : $"Underskott: −{absDiff:F1} t";
                }
                else // Timanställd
                {
                    return $"Totalt {CurrentStats.TotalHours:F1} t";
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
                    return "Lön: Ej angiven";
                if (ActiveJob.EmploymentType == EmploymentType.Permanent)
                {
                    return ActiveJob.MonthlySalary.HasValue
                        ? $"{ActiveJob.MonthlySalary.Value:N0} kr"
                        : "Ej angiven";
                }

                // Timanställd
                return ActiveJob.HourlyRate.HasValue
                    ? $"{ActiveJob.HourlyRate.Value:N0} kr/tim"
                    : "Ej angiven";
            }
        }

        public string TotalHoursText
        {
            get
            {
                if (CurrentStats == null)
                    return "–";

                if (ActiveJob?.EmploymentType == EmploymentType.Permanent)
                {
                    var diff = CurrentStats.TotalHours - CurrentStats.ExpectedHours;
                    return $"{diff:+0.0;-0.0;0.0} t";
                }

                // Timanställd
                return $"{CurrentStats.TotalHours:0.0} t";
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

                return $"-{CurrentStats.TaxAmount:N0} kr";
            }
        }

        public string TotalObHoursText => CurrentStats == null ? "" : $"{CurrentStats.TotalObHours:F1}";
        public Color ObHoursColor => (CurrentStats?.TotalObHours ?? 0) > 0 ? Colors.Green : Colors.Gray;
        public IReadOnlyList<ObDetails> ObDetails =>
            CurrentStats?.ObDetails?
                .Where(x => x.Hours > 0)
                .Select(x => new ObDetails
                {
                    Date = x.Date,
                    Hours = x.Hours,
                    RatePerHour = x.RatePerHour,
                    Category = x.Category,
                    Pay = x.Pay,
                    CategoryName = LocalizationHelper.Translate($"OBCategory_{x.Category}") // Översättning
                }).ToList() ?? new List<ObDetails>();

        public string ObPayText => CurrentStats == null
            ? "0 kr"
            : $"{CurrentStats.ObPay:N0} kr";

        // public string FlexBalanceText => CurrentStats == null ? "" : $"Flex-tid: {CurrentStats.FlexBalance:F1}";
        public string FlexBalanceText
        {
            get
            {
                if (CurrentStats == null)
                    return "";
                var sign = CurrentStats.FlexBalance > 0 ? "+" : "";
                return $"Flexsaldo: {sign}{CurrentStats.FlexBalance:F1} h";
            }
        }

        public string SickDaysText => CurrentStats == null ? "" : $"Sjukdagar: {CurrentStats.SickDays}";

        public string VacationDaysText => CurrentStats == null ? "" : $"Semesterdagar: {CurrentStats.VacationDays}";

        public string VabDaysText => CurrentStats == null ? "" : $"Vård av barn dagar: {CurrentStats.VabDays}";

        // Jour kommer vi lägga till i SalaryStats sen
        public string JourText => CurrentStats == null ? "" : $"Jour: {CurrentStats.JourHours:F1}";

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

        public ICommand PrevMonthCommand => new Command(() =>
        {
            if (!CanGoPrevMonth)
                return;

            SelectedMonth = SelectedMonth.AddMonths(-1);
        });

        public ICommand NextMonthCommand => new Command(() =>
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

            LoadData();
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
            OnPropertyChanged(nameof(HoursSummaryText));
            OnPropertyChanged(nameof(HoursSummaryColor));
            OnPropertyChanged(nameof(BaseSalaryText));
            OnPropertyChanged(nameof(TotalHoursText));

            OnPropertyChanged(nameof(TaxText));

            OnPropertyChanged(nameof(TotalObHoursText));
            OnPropertyChanged(nameof(ObDetails));
            OnPropertyChanged(nameof(ObPayText));
            OnPropertyChanged(nameof(ObHoursColor));

            OnPropertyChanged(nameof(FlexBalanceText));
            OnPropertyChanged(nameof(SickDaysText));
            OnPropertyChanged(nameof(VacationDaysText));
            OnPropertyChanged(nameof(VabDaysText));
            OnPropertyChanged(nameof(JourText));
        }

        private void RefreshStats()
        {
            if (ActiveJob == null)
            {
                CurrentStats = null;
                return;
            }

            CurrentStats = _salaryHandler.CalculateMonthlyStats(ActiveJob.Id, SelectedMonth);
        }
        #endregion

        #region Help Methods

        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
