using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Reports;
using MyWorkSalary.Services.Handlers;
using MyWorkSalary.Services.Interfaces;
using System.ComponentModel;
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
                    CurrentStats = _salaryHandler.CalculateMonthlyStats(_activeJob.Id, DateTime.Now);
                }
                else
                {
                    CurrentStats = null;
                }
                OnPropertyChanged(nameof(HoursSummaryText));
            }
        }

        public bool HasActiveJob => ActiveJob != null;

        public string WelcomeText =>
            HasActiveJob ? $"Aktivt jobb - {ActiveJob.JobTitle}" : "Välkommen till din löneapp!";

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

        #endregion

        // ==== Formaterade properties för XAML ====
        #region UI Text Properties

        public string MonthlySalaryText => CurrentStats == null ? "Timlön" : $"{CurrentStats.TotalSalary:N0} kr";

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
                        return "Exakt enligt schema";

                    var sign = diff > 0 ? "+" : "-";
                    var absDiff = Math.Abs(diff);
                    return $"{sign}{absDiff:F1} timmar {(diff > 0 ? "till timbank" : "från timbank")}";
                }
                else // Timanställd
                {
                    return $"För {CurrentStats.TotalHours:F1} timmar";
                }
            }
        }

        public string BaseSalaryText
        {
            get
            {
                if (ActiveJob == null)
                    return "Ingen aktiv anställning";

                if (ActiveJob.EmploymentType == EmploymentType.Permanent && ActiveJob.MonthlySalary.HasValue)
                    return $"Grund lön: {ActiveJob.MonthlySalary.Value:N0} kr/mån";

                if (ActiveJob.HourlyRate.HasValue)
                    return $"Timlön: {ActiveJob.HourlyRate.Value:N0} kr/timme";

                return "Lön ej angiven";
            }
        }

        public string TotalHoursText => CurrentStats == null ? "" : $"{CurrentStats.TotalHours:F1} timmar denna månad";

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

        // public decimal ObPayText => CurrentStats?.ObPay ?? 0;
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
        #endregion

        #region Constructor
        public SalaryPageViewModel(IDashboardService dashboardService,
            IJobProfileRepository jobProfileRepository,
            SalaryStatsHandler salaryStatsHandler)
        {
            _dashboardService = dashboardService;
            _jobProfileRepository = jobProfileRepository;
            _salaryHandler = salaryStatsHandler;

            LoadData();
        }
        #endregion

        #region Private Methods
        public async Task LoadData()
        {
            ActiveJob = _jobProfileRepository.GetActiveJob();
            if (!HasActiveJob)
                return;

            CurrentStats = _salaryHandler.CalculateMonthlyStats(ActiveJob.Id, DateTime.Now);

        }

        private void NotifyStatsBindingsChanged()
        {
            OnPropertyChanged(nameof(MonthlySalaryText));
            OnPropertyChanged(nameof(HoursSummaryText));
            OnPropertyChanged(nameof(BaseSalaryText));
            OnPropertyChanged(nameof(TotalHoursText));

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
