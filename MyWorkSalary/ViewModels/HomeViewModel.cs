using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Services;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Views.Pages;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        #region Private Fields
        private readonly DatabaseService _databaseService;
        private readonly IDashboardService _dashboardService;
        private JobProfile _activeJob;
        private bool _hasActiveJob;
        private decimal _monthlyHours;
        private decimal _monthlyObHours;
        private int _workDaysThisMonth;
        private decimal _expectedHoursThisMonth;
        private ObservableCollection<RecentActivityItem> _recentActivities;

        // Flex fields
        private decimal _currentFlexBalance;
        private string _flexStatusIcon;
        private decimal _monthlyFlexDifference;
        private string _monthlyFlexText;
        private bool _hasFlexTime;
        #endregion

        #region Constructor
        public HomeViewModel(DatabaseService databaseService, IDashboardService dashboardService)
        {
            _databaseService = databaseService;
            _dashboardService = dashboardService;

            // Commands
            SetupJobCommand = new Command(OnSetupJob);
            AddShiftCommand = new Command(OnAddShift, () => HasActiveJob);
            ViewReportsCommand = new Command(OnViewReports, () => HasActiveJob);

            // Initialize collections
            RecentActivities = new ObservableCollection<RecentActivityItem>();
            RecentActivities.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(RecentActivitiesHeight));
            };

        }
        #endregion

        #region Existing Properties
        public JobProfile ActiveJob
        {
            get => _activeJob;
            set
            {
                _activeJob = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WelcomeText));
                OnPropertyChanged(nameof(HasActiveJob));
                OnPropertyChanged(nameof(ShowSetupButton));
                OnPropertyChanged(nameof(ShowDashboard));
                OnPropertyChanged(nameof(JobDisplayText));
                OnPropertyChanged(nameof(SalaryTypeInfo));
                OnPropertyChanged(nameof(WorkplaceText)); 
                OnPropertyChanged(nameof(HasFlexTime));
                OnPropertyChanged(nameof(FlexBalanceText));
            }
        }

        public bool HasActiveJob
        {
            get => _hasActiveJob;
            set
            {
                _hasActiveJob = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowSetupButton));
                OnPropertyChanged(nameof(ShowDashboard));

                // Uppdatera command states
                ((Command)AddShiftCommand).ChangeCanExecute();
                ((Command)ViewReportsCommand).ChangeCanExecute();
            }
        }

        public string WelcomeText => HasActiveJob && ActiveJob != null
            ? $"{LocalizationHelper.Translate("ActiveJob")} - {ActiveJob.JobTitle}"
            : LocalizationHelper.Translate("WelcomeToApp");

        public string WorkplaceText => ActiveJob?.Workplace ?? "";
        public bool ShowSetupButton => !HasActiveJob;
        public bool ShowDashboard => HasActiveJob;

        // Jobb info 
        public string JobDisplayText => ActiveJob?.JobTitle ?? "";
        public string SalaryTypeInfo
        {
            get
            {
                if (ActiveJob?.ExpectedHoursPerMonth > 0)
                {
                    return $"{LocalizationHelper.Translate("MonthlySalary")} • {ActiveJob.ExpectedHoursPerMonth} {LocalizationHelper.Translate("HoursPerMonth")} • {LocalizationHelper.Translate("FlexTime")}";
                }
                return $"{LocalizationHelper.Translate("HourlyWage")} • {LocalizationHelper.Translate("NoFlexTime")}";
            }
        }

        // Månadens statistik
        public string MonthlyHoursText => MonthlyHours.ToString("F1");
        public decimal MonthlyHours
        {
            get => _monthlyHours;
            set
            {
                _monthlyHours = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MonthlyHoursText));
                OnPropertyChanged(nameof(FlexBalanceText));
            }
        }

        // OB som betalas denna månad (från föregående månad)
        private decimal _obHoursPaidThisMonth;
        public decimal ObHoursPaidThisMonth
        {
            get => _obHoursPaidThisMonth;
            set
            {
                _obHoursPaidThisMonth = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ObHoursPaidThisMonthText));
            }
        }

        public string ObHoursPaidThisMonthText => ObHoursPaidThisMonth.ToString("F1");

        // OB arbetade denna månad (du har redan MonthlyObHours)
        public string MonthlyObHoursText => MonthlyObHours.ToString("F1");

        public decimal MonthlyObHours
        {
            get => _monthlyObHours;
            set
            {
                _monthlyObHours = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MonthlyObHoursText));
            }
        }

        public int WorkDaysThisMonth
        {
            get => _workDaysThisMonth;
            set
            {
                _workDaysThisMonth = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<RecentActivityItem> RecentActivities
        {
            get => _recentActivities;
            set
            {
                _recentActivities = value;
                OnPropertyChanged();
            }
        }

        // Display properties
        public string CurrentMonthYear
        {
            get
            {
                var culture = CultureInfo.CurrentCulture;
                var monthYear = DateTime.Now.ToString("MMMM yyyy", culture);
                return culture.TextInfo.ToTitleCase(monthYear);
            }
        }
        public string TodayDate => DateTime.Now.ToString("dddd d MMMM yyyy", CultureInfo.CurrentCulture);

        public double RecentActivitiesHeight
        {
            get
            {
                if (RecentActivities == null || RecentActivities.Count == 0)
                    return 150; // tom höjd
                return RecentActivities.Count * 60; // ca 60 px per rad
            }
        }
        #endregion

        #region FlexTime Properties
        /// <summary>
        /// Aktuellt totalt flex-saldo
        /// </summary>
        public decimal CurrentFlexBalance
        {
            get => _currentFlexBalance;
            set
            {
                _currentFlexBalance = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Formaterad text för flex-saldo ("+12.5t kompledighet")
        /// </summary>
        public string FlexBalanceText
        {
            get
            {
                if (!HasFlexTime || ActiveJob == null)
                    return "";

                var kvar = ExpectedHoursThisMonth - MonthlyHours;
                if (kvar > 0)
                    return $"{kvar:F1} {LocalizationHelper.Translate("HoursOwed")}";
                else if (kvar < 0)
                    return $"{Math.Abs(kvar):F1} {LocalizationHelper.Translate("HoursCredit")}";
                else
                    return LocalizationHelper.Translate("MonthlyGoalReached");
            }
        }

        public decimal ExpectedHoursThisMonth
        {
            get => _expectedHoursThisMonth;
            set
            {
                _expectedHoursThisMonth = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FlexBalanceText));
            }
        }

        /// <summary>
        /// Ikon för flex-status (📈📉⚖️)
        /// </summary>
        public string FlexStatusIcon
        {
            get => _flexStatusIcon;
            set
            {
                _flexStatusIcon = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Denna månads flex-skillnad
        /// </summary>
        public decimal MonthlyFlexDifference
        {
            get => _monthlyFlexDifference;
            set
            {
                _monthlyFlexDifference = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Text för månadsvis flex ("+2.5t denna månad")
        /// </summary>
        public string MonthlyFlexText
        {
            get => _monthlyFlexText;
            set
            {
                _monthlyFlexText = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Om jobbet har flex-tid (månadslön)
        /// </summary>
        public bool HasFlexTime
        {
            get => _hasFlexTime;
            set
            {
                _hasFlexTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowFlexSection));
                OnPropertyChanged(nameof(FlexBalanceText));
            }
        }

        /// <summary>
        /// Visa flex-sektion i UI
        /// </summary>
        public bool ShowFlexSection => HasActiveJob && HasFlexTime;

        /// <summary>
        /// Färg för flex-saldo (grön/röd/grå)
        /// </summary>
        public Color FlexBalanceColor => CurrentFlexBalance switch
        {
            > 0 => Colors.Green,
            < 0 => Colors.Red,
            _ => Colors.Gray
        };
        #endregion

        #region Commands
        public ICommand SetupJobCommand { get; }
        public ICommand AddShiftCommand { get; }
        public ICommand ViewReportsCommand { get; }
        #endregion

        #region Methods
        private void LoadDashboardData()
        {
            LoadActiveJob();
            if (HasActiveJob)
            {
                LoadMonthlyStats();
                LoadRecentActivities();
                LoadFlexTimeData();
            }
        }

        private void LoadActiveJob()
        {
            var jobs = _databaseService.JobProfiles.GetJobProfiles();
            var activeJob = jobs.FirstOrDefault(j => j.IsActive);
            ActiveJob = activeJob;
            HasActiveJob = activeJob != null;

            // Kontrollera om jobbet har flex-tid
            HasFlexTime = activeJob?.ExpectedHoursPerMonth > 0;
        }

        private void LoadMonthlyStats()
        {
            if (ActiveJob == null)
                return;

            var stats = _dashboardService.GetMonthlyStats(ActiveJob.Id);
            MonthlyHours = stats.TotalHours;
            MonthlyObHours = stats.TotalObHours;
            WorkDaysThisMonth = stats.WorkDays;
            ExpectedHoursThisMonth = stats.ExpectedHours;

            // OB som betalas denna månad
            ObHoursPaidThisMonth = _dashboardService.GetPreviousMonthObHours(ActiveJob.Id);

            // Uppdatera flex när shifts ändras
            if (HasFlexTime)
            {
                _dashboardService.UpdateCurrentMonthFlexBalance(ActiveJob.Id);
            }
        }

        private void LoadRecentActivities()
        {
            if (ActiveJob == null)
                return;

            var activities = _dashboardService.GetRecentActivities(ActiveJob.Id, 4);  // <-- 4 max aktiviteter
            RecentActivities.Clear();
            foreach (var activity in activities)
            {
                RecentActivities.Add(activity);
            }
            // Uppdatera HeightRequest
            OnPropertyChanged(nameof(RecentActivitiesHeight));
        }

        /// <summary>
        /// Ladda flex-tid data 
        /// </summary>
        private void LoadFlexTimeData()
        {
            if (ActiveJob == null || !HasFlexTime)
                return;

            try
            {
                var flexStatus = _dashboardService.GetFlexStatus(ActiveJob.Id);

                // Hämta och visa aktuellt saldo
                CurrentFlexBalance = flexStatus.CurrentBalance; 
                FlexStatusIcon = flexStatus.StatusIcon;
                MonthlyFlexDifference = flexStatus.MonthlyDifference;
                MonthlyFlexText = flexStatus.MonthlyText;

                // Hämta föregående månads diff
                var previousMonth = DateTime.Now.AddMonths(-1);
                var prevBalance = _dashboardService.GetFlexTimeHistory(ActiveJob.Id, 2)
                    .FirstOrDefault(f => f.Month == previousMonth.Month && f.Year == previousMonth.Year);

                OnPropertyChanged(nameof(FlexBalanceColor));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i LoadFlexTimeData: {ex.Message}");

                // Fallback values
                CurrentFlexBalance = 0;
                FlexStatusIcon = "⚠️";
                MonthlyFlexDifference = 0;
                MonthlyFlexText = "";
            }
        }

        public void RefreshData()
        {
            LoadDashboardData();
        }

        // Command handlers
        private async void OnSetupJob()
        {
            await Shell.Current.GoToAsync("//SettingsPage");
            await Shell.Current.GoToAsync(nameof(AddJobPage));
        }

        private async void OnAddShift()
        {
            await Shell.Current.GoToAsync(nameof(AddShiftPage));
        }

        private async void OnViewReports()
        {
            await Shell.Current.GoToAsync("//SalaryPage");
        }
        #endregion
    }
}
