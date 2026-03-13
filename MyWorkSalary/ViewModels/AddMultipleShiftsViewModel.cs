using MyWorkSalary.Helpers.Calendar;
using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Services.Premium;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public class AddMultipleShiftsViewModel : BaseViewModel
    {
        #region Private Fields
        private readonly IJobProfileRepository _jobProfileRepository;
        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly IShiftCalculationService _calculationService;
        private readonly IOBEventService _obEventService;
        private readonly IPremiumService _premiumService;
        private ObservableCollection<CalendarWeek> _calendarWeeks;
        private readonly AdService _adService;

        private JobProfile _activeJob;
        private TimeSpan _startTime;
        private TimeSpan _endTime;
        private int _selectedDaysCount;
        private bool _canSave;
        private DateTime _currentMonth;
        private int _breakMinutes;
        private string _notes = string.Empty;
        #endregion

        #region Constructor
        public AddMultipleShiftsViewModel(
            IJobProfileRepository jobProfileRepository,
            IWorkShiftRepository workShiftRepository,
            IShiftCalculationService calculationService,
            IOBEventService obEventService,
            IPremiumService premiumService,
            AdService adService)
        {
            _jobProfileRepository = jobProfileRepository;
            _workShiftRepository = workShiftRepository;
            _calculationService = calculationService;
            _obEventService = obEventService;
            _premiumService = premiumService;
            _adService = adService;

            // Starta loading
            IsBusy = true;

            // Ladda asynkront
            _ = Task.Run(async () =>
            {
                MainThread.BeginInvokeOnMainThread(() => IsBusy = false);
            });

            // Initiera värden
            _currentMonth = DateTime.Today;
            _startTime = new TimeSpan(8, 0, 0); // 08:00
            _endTime = new TimeSpan(16, 0, 0);  // 16:00

            // Commands
            SaveCommand = new Command(OnSave, CanExecuteSave);
            CancelCommand = new Command(OnCancel);

            ToggleDayCommand = new Command<CalendarDay>(OnToggleDay);
            PreviousMonthCommand = new Command(OnPreviousMonth);
            NextMonthCommand = new Command(OnNextMonth);

            LoadActiveJob();

            LocalizationHelper.LanguageChanged += () =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnPropertyChanged(nameof(WeekDays));
                    OnPropertyChanged(nameof(SelectedDaysText));
                    OnPropertyChanged(nameof(MonthText));
                });
            };
        }
        #endregion

        #region Properties
        public JobProfile ActiveJob
        {
            get => _activeJob;
            set
            {
                _activeJob = value;
                OnPropertyChanged();
            }
        }
        public string ActiveJobTitle => ActiveJob?.JobTitle ?? LocalizationHelper.Translate("RegularShift_Validation_NoJobSelected");

        public TimeSpan StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged();
                ValidateAllDays();
                UpdateCanSave();
            }
        }
        public TimeSpan EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged();
                ValidateAllDays();
                UpdateCanSave();
            }
        }
        public int BreakMinutes
        {
            get => _breakMinutes;
            set
            {
                _breakMinutes = value;
                OnPropertyChanged();
                ValidateAllDays();
                UpdateCanSave();
            }
        }
        public string Notes
        {
            get => _notes;
            set
            {
                _notes = value;
                OnPropertyChanged();
            }
        }

        public int SelectedDaysCount
        {
            get => _selectedDaysCount;
            set
            {
                _selectedDaysCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedDaysText));
            }
        }
        public string SelectedDaysText => $"{LocalizationHelper.Translate("Calendar_SelectedDays")}: {SelectedDaysCount}";

        public bool CanSave
        {
            get => _canSave;
            set
            {
                _canSave = value;
                OnPropertyChanged();
                ((Command)SaveCommand).ChangeCanExecute();
            }
        }

        public DateTime CurrentMonth
        {
            get => _currentMonth;
            set
            {
                _currentMonth = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MonthText));
                // TODO: Reset valda dagar här senare
            }
        }
        public string MonthText => CurrentMonth.ToString("MMMM yyyy");
        public List<string> WeekDays => CalendarGenerator.GetWeekDayNames();
        public ObservableCollection<CalendarWeek> CalendarWeeks
        {
            get => _calendarWeeks;
            set
            {
                _calendarWeeks = value;
                OnPropertyChanged();
            }
        }
        public IEnumerable<CalendarDay> CalendarDays => CalendarWeeks.SelectMany(w => w.Days);

        public bool IsNotBusy => !IsBusy;
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }
        #endregion

        #region Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ICommand ToggleDayCommand { get; }
        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        #endregion

        #region Methods
        private void LoadActiveJob()
        {
            try
            {
                ActiveJob = _jobProfileRepository.GetActiveJob();
                if (ActiveJob == null)
                {
                    // TODO: Hantera inget aktivt jobb
                }

                GenerateCalendar();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading active job: {ex.Message}");
            }
        }

        private bool CanExecuteSave()
        {
            return CanSave && SelectedDaysCount > 0 && ActiveJob != null;
        }

        private async void OnSave()
        {
            try
            {
                IsBusy = true; // Starta busy state

                var selectedDays = CalendarDays
                    .Where(d => d.IsSelected && d.IsCurrentMonth)
                    .ToList();

                if (!selectedDays.Any())
                {
                    await Shell.Current.DisplayAlert("Info", "Inga dagar valda", "OK");
                    return;
                }

                // Spara alla pass i databasen
                int savedCount = 0;

                foreach (var day in selectedDays)
                {
                    // Beräkna detaljerat resultat (som i RegularShiftViewModel)
                    var result = _calculationService.CalculateRegularShiftDetailed(
                        day.Date,
                        StartTime,
                        EndTime,
                        ActiveJob,
                        BreakMinutes
                    );

                    var workShift = new WorkShift
                    {
                        JobProfileId = ActiveJob.Id,
                        ShiftDate = day.Date,
                        ShiftType = ShiftType.Regular,
                        StartTime = day.Date.Add(StartTime),
                        EndTime = EndTime > StartTime
                            ? day.Date.Add(EndTime)
                            : day.Date.AddDays(1).Add(EndTime), // Hantera nattpass
                        BreakMinutes = BreakMinutes,
                        Notes = Notes, // Här är Notes!
                        CreatedDate = DateTime.Now,

                        // Timmar
                        TotalHours = result.TotalHours,
                        RegularHours = result.RegularHours,

                        // Löner
                        RegularPay = result.RegularPay,
                        TotalPay = result.RegularPay,

                        // OB (noll för nu)
                        EveningOBRate = 0,
                        NightOBRate = 0,
                        EveningOBPay = 0,
                        NightOBPay = 0,
                        OBPay = 0,

                        // Holiday
                        IsHoliday = false, // Kan utökas senare
                        IsBigHoliday = false,

                        // Extra shift
                        IsExtraShift = false,
                        ExtraShiftPay = 0
                    };

                    // Spara via repository
                    var savedShift = _workShiftRepository.SaveWorkShift(workShift);

                    if (savedShift != null && savedShift.Id > 0)
                    {
                        savedCount++;

                        // Försök spara OB-händelser
                        try
                        {
                            var obSummary = _obEventService.RebuildForWorkShift(savedShift);
                            savedShift.OBHours = obSummary.TotalObHours;
                            savedShift.OBPay = obSummary.TotalObPay;
                            savedShift.TotalPay = savedShift.RegularPay + savedShift.OBPay + savedShift.ExtraShiftPay;
                            _workShiftRepository.SaveWorkShift(savedShift);
                        }
                        catch
                        {
                            // Ignorera OB-fel, fortsätt spara
                        }
                    }
                }

                await Shell.Current.DisplayAlert(
                    "Klart",
                    $"{savedCount} av {selectedDays.Count} pass har sparats",
                    "OK");

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Fel",
                    $"Kunde inte spara pass: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false; // Avsluta busy state
            }
        }

        private async void OnCancel()
        {
            await Shell.Current.GoToAsync("..");
        }

        private void OnToggleDay(CalendarDay day)
        {
            if (day.IsCurrentMonth && !day.HasConflict)
            {
                day.IsSelected = !day.IsSelected;
                UpdateSelectedDaysCount();
                UpdateCanSave();
            }
        }

        private void OnPreviousMonth()
        {
            CurrentMonth = CurrentMonth.AddMonths(-1);
            GenerateCalendar();
        }

        private void OnNextMonth()
        {
            CurrentMonth = CurrentMonth.AddMonths(1);
            GenerateCalendar();
        }

        public void GenerateCalendar()
        {
            var weeks = CalendarGenerator.GenerateMonth(CurrentMonth);

            foreach (var week in weeks)
            {
                foreach (var day in week.Days.Where(d => d.IsCurrentMonth))
                {
                    day.HasConflict = CalendarValidator.HasConflict(
                        day.Date,
                        StartTime,
                        EndTime,
                        _workShiftRepository,
                        ActiveJob.Id);
                }
            }

            CalendarWeeks = new ObservableCollection<CalendarWeek>(weeks);
            UpdateCanSave();
        }

        private void UpdateSelectedDaysCount()
        {
            SelectedDaysCount = CalendarWeeks
                .SelectMany(w => w.Days)
                .Count(d => d.IsSelected && d.IsCurrentMonth);
        }

        private void ValidateAllDays()
        {
            foreach (var day in CalendarWeeks
                .SelectMany(w => w.Days)
                .Where(d => d.IsCurrentMonth))
            {
                day.HasConflict = CalendarValidator.HasConflict(
                    day.Date,
                    StartTime,
                    EndTime,
                    _workShiftRepository,
                    ActiveJob.Id);
            }
        }

        private void UpdateCanSave()
        {
            var selectedDays = CalendarDays
                .Where(d => d.IsSelected && d.IsCurrentMonth)
                .ToList();

            var hasConflicts = selectedDays.Any(d => d.HasConflict);
            var hasSelectedDays = selectedDays.Any();

            CanSave = hasSelectedDays && !hasConflicts && ActiveJob != null;
        }

        public void RefreshWeekDays()
        {
            OnPropertyChanged(nameof(WeekDays));
        }
        #endregion
    }
}