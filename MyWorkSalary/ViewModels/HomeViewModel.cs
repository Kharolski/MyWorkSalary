using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Services;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Views.Pages;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
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
        private decimal _totalFlexBalanceExcludingCurrentMonth;
        private ObservableCollection<RecentActivityItem> _recentActivities;

        // Flex fields
        private decimal _currentFlexBalance;
        private string _flexStatusIcon;
        private decimal _monthlyFlexDifference;
        private string _monthlyFlexText;
        private decimal _previousMonthFlexDifference;
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
            ? $"Aktivt jobb - {ActiveJob.JobTitle}"
            : "Välkommen till din löneapp!";

        public string WorkplaceText => ActiveJob?.Workplace ?? "";
        public bool ShowSetupButton => !HasActiveJob;
        public bool ShowDashboard => HasActiveJob;

        // Jobb info 
        public string JobDisplayText => ActiveJob?.JobTitle ?? "";
        public string SalaryTypeInfo => ActiveJob?.ExpectedHoursPerMonth > 0
            ? $"Månadslön • {ActiveJob.ExpectedHoursPerMonth} tim/månad • Flex-tid"
            : "Timlön • Ingen flex-tid";

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
                var culture = new System.Globalization.CultureInfo("sv-SE");
                var monthYear = DateTime.Now.ToString("MMMM yyyy", culture);

                // Använd TextInfo för korrekt kapitalisering
                return culture.TextInfo.ToTitleCase(monthYear);
            }
        }
        public string TodayDate => DateTime.Now.ToString("dddd d MMMM yyyy", new System.Globalization.CultureInfo("sv-SE"));
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
                    return $"{kvar:F1} timmar skuld";
                else if (kvar < 0)
                    return $"{Math.Abs(kvar):F1} timmar kompledighet";
                else
                    return "Månadens mål uppnått!";
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

        // Föregående månad (förändring)
        public decimal PreviousMonthFlexDifference
        {
            get => _previousMonthFlexDifference;
            set
            {
                _previousMonthFlexDifference = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PreviousMonthFlexText));
            }
        }
        public string PreviousMonthFlexText => $"{PreviousMonthFlexDifference:F1} tim förra månaden";

        public decimal TotalFlexBalanceExcludingCurrentMonth
        {
            get => _totalFlexBalanceExcludingCurrentMonth;
            set
            {
                _totalFlexBalanceExcludingCurrentMonth = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalFlexBalanceText));
            }
        }

        // Total flex-saldo (hela anställningen)
        public string TotalFlexBalanceText => $"{TotalFlexBalanceExcludingCurrentMonth:F1} tim saldo";

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

                PreviousMonthFlexDifference = prevBalance?.MonthlyDifference ?? 0;

                TotalFlexBalanceExcludingCurrentMonth = _dashboardService.GetTotalFlexBalanceExcludingCurrentMonth(ActiveJob.Id);

                OnPropertyChanged(nameof(FlexBalanceColor));
                OnPropertyChanged(nameof(TotalFlexBalanceText)); // <-- tvinga UI att uppdatera
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

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
