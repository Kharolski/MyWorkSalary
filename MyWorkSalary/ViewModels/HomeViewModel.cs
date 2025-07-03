using MyWorkSalary.Models;
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
        private decimal _monthlyEarnings;
        private int _workDaysThisMonth;
        private ObservableCollection<RecentActivityItem> _recentActivities;

        // Flex fields
        private decimal _currentFlexBalance;
        private string _flexBalanceText;
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
            ViewFlexHistoryCommand = new Command(OnViewFlexHistory, () => HasActiveJob && HasFlexTime); 

            // Initialize collections
            RecentActivities = new ObservableCollection<RecentActivityItem>();

            // Ladda data
            LoadDashboardData();
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
                OnPropertyChanged(nameof(HasFlexTime)); 
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
                ((Command)ViewFlexHistoryCommand).ChangeCanExecute(); 
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
        public decimal MonthlyHours
        {
            get => _monthlyHours;
            set
            {
                _monthlyHours = value;
                OnPropertyChanged();
            }
        }

        public decimal MonthlyEarnings
        {
            get => _monthlyEarnings;
            set
            {
                _monthlyEarnings = value;
                OnPropertyChanged();
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
        public string CurrentMonthYear => DateTime.Now.ToString("MMMM yyyy", new System.Globalization.CultureInfo("sv-SE"));
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
        /// Formaterad text för flex-saldo ("+12.5h kompledighet")
        /// </summary>
        public string FlexBalanceText
        {
            get => _flexBalanceText;
            set
            {
                _flexBalanceText = value;
                OnPropertyChanged();
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
        /// Text för månadsvis flex ("+2.5h denna månad")
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
                ((Command)ViewFlexHistoryCommand).ChangeCanExecute();
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
        public ICommand ViewFlexHistoryCommand { get; } 
        #endregion

        #region Methods
        private string GetSalaryDisplayText(JobProfile job)
        {
            if (job.HourlyRate.HasValue)
            {
                var employmentText = job.EmploymentType switch
                {
                    EmploymentType.Permanent => "Tillsvidare",
                    EmploymentType.Temporary => "Visstid",
                    EmploymentType.OnCall => "Timanställd",
                    _ => "Anställd"
                };
                return $"{job.HourlyRate:C}/tim • {employmentText}";
            }
            else if (job.MonthlySalary.HasValue)
            {
                return $"{job.MonthlySalary:C}/mån • Månadslön";
            }
            return "Lön ej angiven";
        }

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
            var jobs = _databaseService.GetJobProfiles();
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
            MonthlyEarnings = stats.TotalEarnings;
            WorkDaysThisMonth = stats.WorkDays;

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

            var activities = _dashboardService.GetRecentActivities(ActiveJob.Id, 4);
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

                CurrentFlexBalance = flexStatus.CurrentBalance;
                FlexBalanceText = flexStatus.BalanceText;
                FlexStatusIcon = flexStatus.StatusIcon;
                MonthlyFlexDifference = flexStatus.MonthlyDifference;
                MonthlyFlexText = flexStatus.MonthlyText;

                OnPropertyChanged(nameof(FlexBalanceColor));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i LoadFlexTimeData: {ex.Message}");

                // Fallback values
                CurrentFlexBalance = 0;
                FlexBalanceText = "Kunde inte ladda flex-data";
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

        /// <summary>
        /// Visa flex-historik 
        /// </summary>
        private async void OnViewFlexHistory()
        {
            // TODO: Skapa FlexHistoryPage
            await Shell.Current.DisplayAlert("Flex-historik",
                $"Aktuellt saldo: {FlexBalanceText}\n{MonthlyFlexText}",
                "OK");
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
