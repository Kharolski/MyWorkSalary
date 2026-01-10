using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;
using System.ComponentModel;
using System.Windows.Input;
using MyWorkSalary.Services.Repositories;

namespace MyWorkSalary.ViewModels.ShiftTypes
{
    public class RegularShiftViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly IShiftCalculationService _calculationService;
        private readonly IShiftTimeSettingsRepository _shiftTimeSettingsRepository;
        private readonly IOBEventRepository _obEventRepository;

        private JobProfile _activeJob;
        private DateTime _selectedDate = DateTime.Today;
        private TimeSpan _startTime = new TimeSpan(8, 0, 0);
        private TimeSpan _endTime = new TimeSpan(16, 0, 0);
        private int _breakMinutes = 0;
        private string _breakSuggestionText = "";
        private string _notes = string.Empty;
        private bool _isExtraShift = false;
        private bool _showCalculation;
        private bool _canSave;
        private decimal _calculatedHours;
        private decimal _calculatedPay;
        #endregion

        #region Constructor
        public RegularShiftViewModel(
            IWorkShiftRepository workShiftRepository,
            IShiftCalculationService calculationService,
            IShiftTimeSettingsRepository shiftTimeSettingsRepository,
            IOBEventRepository obEventRepository)
        {
            _workShiftRepository = workShiftRepository;
            _calculationService = calculationService;
            _shiftTimeSettingsRepository = shiftTimeSettingsRepository;
            _obEventRepository = obEventRepository;

            SaveCommand = new Command(async () => await SaveRegularShift(), () => CanSave);
            CalculateHours();
        }
        #endregion

        #region Properties
        public JobProfile ActiveJob
        {
            get => _activeJob;
            set
            {
                _activeJob = value;
                if (_activeJob != null)
                {
                    // Load ShiftTimeSettings when ActiveJob is set
                    _activeJob.ShiftTimeSettings = _shiftTimeSettingsRepository.GetForJob(_activeJob.Id);
                }
                OnPropertyChanged();
                CalculateHours();
            }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
                CalculateHours();
            }
        }

        public TimeSpan StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged();
                CalculateHours();
            }
        }

        public TimeSpan EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged();
                CalculateHours();
            }
        }

        public int BreakMinutes
        {
            get => _breakMinutes;
            set
            {
                _breakMinutes = value;
                OnPropertyChanged();
                CalculateHours();
            }
        }

        public string BreakSuggestionText
        {
            get => _breakSuggestionText;
            set
            {
                _breakSuggestionText = value;
                OnPropertyChanged();
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

        public bool IsExtraShift
        {
            get => _isExtraShift;
            set
            {
                _isExtraShift = value;
                OnPropertyChanged();
            }
        }

        public string CalculationSummary
        {
            get
            {
                if (BreakMinutes > 0)
                {
                    return string.Format(
                        LocalizationHelper.Translate("RegularShift_Calculation_WithBreak"),
                        CalculatedHours,
                        BreakMinutes);
                }
                return string.Format(
                    LocalizationHelper.Translate("RegularShift_Calculation_WithoutBreak"),
                    CalculatedHours);
            }
        }

        public decimal CalculatedHours
        {
            get => _calculatedHours;
            set
            {
                _calculatedHours = value;
                OnPropertyChanged();
            }
        }

        public bool ShowCalculation
        {
            get => _showCalculation;
            set
            {
                _showCalculation = value;
                OnPropertyChanged();
            }
        }

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
        #endregion

        #region Commands
        public ICommand SaveCommand { get; }
        #endregion

        #region Calculation & Validation
        private void CalculateHours()
        {
            if (ActiveJob == null)
            {
                CalculatedHours = 0;
                ShowCalculation = false;
                CanSave = false;
                return;
            }

            // Initialize ShiftTimeSettings if null
            if (ActiveJob.ShiftTimeSettings == null)
            {
                ActiveJob.ShiftTimeSettings = new ShiftTimeSettings
                {
                    // Vardagar
                    DayStart = new TimeSpan(6, 0, 0),     // 06:00
                    EveningStart = new TimeSpan(18, 0, 0), // 18:00
                    NightStart = new TimeSpan(22, 0, 0),   // 22:00
                    EveningActive = true,
                    NightActive = true,
                    
                    // Helger (samma tider som vardagar som default)
                    WeekendDayStart = new TimeSpan(6, 0, 0),
                    WeekendEveningStart = new TimeSpan(18, 0, 0),
                    WeekendNightStart = new TimeSpan(22, 0, 0),
                    WeekendEveningActive = true,
                    WeekendNightActive = true
                };
            }

            var eveningRate = GetEveningOBRate();
            var nightRate = GetNightOBRate();

            var result = _calculationService.CalculateRegularShiftDetailed(
                SelectedDate,
                StartTime,
                EndTime,
                ActiveJob,
                ActiveJob.ShiftTimeSettings,
                eveningRate,
                nightRate,
                BreakMinutes);

            CalculatedHours = result.TotalHours;

            UpdateBreakSuggestion();

            ShowCalculation = result.TotalHours > 0;
            CanSave = ValidateRegularShift();

        }

        private decimal GetEveningOBRate()
        {
            return ActiveJob?.OBRates?
                .FirstOrDefault(r =>
                    r.Category == OBCategory.Evening &&
                    r.IsActive)
                ?.RatePerHour ?? 0m;
        }

        private decimal GetNightOBRate()
        {
            return ActiveJob?.OBRates?
                .FirstOrDefault(r =>
                    r.Category == OBCategory.Night &&
                    r.IsActive)
                ?.RatePerHour ?? 0m;
        }

        private bool ValidateRegularShift()
        {
            var totalHours = CalculatedHours + (BreakMinutes / 60m);
            return CalculatedHours > 0 && 
                   _calculationService.ValidateHours(CalculatedHours) &&
                   _calculationService.ValidateBreakMinutes(BreakMinutes, totalHours);
        }

        private void UpdateBreakSuggestion()
        {
            var totalHours = (EndTime - StartTime).TotalHours;
            if (EndTime < StartTime)
                totalHours += 24; // Pass över midnatt

            BreakSuggestionText = _calculationService.GetBreakSuggestionText((decimal)totalHours);

        }
        #endregion

        #region Save Logic
        public async Task<bool> SaveRegularShift()
        {
            try
            {
                if (ActiveJob == null || ActiveJob.ShiftTimeSettings == null)
                    return false;

                // Hämta kväll/natt OB rates
                var eveningRate = GetEveningOBRate();
                var nightRate = GetNightOBRate();

                // Beräkna detaljerat resultat
                var result = _calculationService.CalculateRegularShiftDetailed(
                    SelectedDate,
                    StartTime,
                    EndTime,
                    ActiveJob,
                    ActiveJob.ShiftTimeSettings,
                    eveningRate,
                    nightRate,
                    BreakMinutes
                );

                var workShift = new WorkShift
                {
                    JobProfileId = ActiveJob.Id,
                    ShiftDate = SelectedDate,
                    ShiftType = ShiftType.Regular,
                    StartTime = SelectedDate.Date.Add(StartTime),
                    EndTime = EndTime > StartTime
                        ? SelectedDate.Date.Add(EndTime)
                        : SelectedDate.Date.AddDays(1).Add(EndTime), // Hantera nattpass
                    BreakMinutes = BreakMinutes,

                    // Timmar
                    TotalHours = result.TotalHours,
                    RegularHours = result.RegularHours,
                    EveningHours = result.EveningHours,
                    NightHours = result.NightHours,

                    // Löner
                    RegularPay = result.RegularPay,
                    EveningOBRate = result.EveningOBRate,
                    NightOBRate = result.NightOBRate,
                    EveningOBPay = result.EveningOBPay,
                    NightOBPay = result.NightOBPay,
                    OBPay = result.EveningOBPay + result.NightOBPay,
                    TotalPay = result.RegularPay + result.EveningOBPay + result.NightOBPay,

                    // Snapshot för UI / historik
                    EveningStartAtThatTime = result.EveningStart,
                    NightStartAtThatTime = result.NightStart,
                    EveningActiveAtThatTime = result.EveningActive,
                    NightActiveAtThatTime = result.NightActive,

                    Notes = Notes,
                    CreatedDate = DateTime.Now,
                    IsExtraShift = IsExtraShift
                };

                // Spara via repository
                var savedShift = _workShiftRepository.SaveWorkShift(workShift);
                if (savedShift == null || savedShift.Id <= 0)
                {
                    System.Diagnostics.Debug.WriteLine("Kunde inte spara passet");
                    return false;
                }

                // Försök spara OB-händelser, men låt inte det påverka huvudresultatet
                try
                {
                    CreateAndSaveOBEvents(savedShift);
                }
                catch (Exception obEx)
                {
                    // Logga felet men låt inte det påverka huvudsparandet
                    System.Diagnostics.Debug.WriteLine($"Varning: Kunde inte spara OB-händelser: {obEx.Message}");
                }

                // Returnera baserat på om passet sparades, oavsett OB-händelser
                return savedShift != null && savedShift.Id > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fel vid sparande av pass: {ex.Message}");
                return false;
            }
        }

        private void CreateAndSaveOBEvents(WorkShift workShift)
        {
            if (workShift == null || workShift.JobProfileId <= 0)
                return;
            try
            {
                // Ta bort gamla OB-händelser för detta pass (om vi uppdaterar)
                if (workShift.Id != 0)
                {
                    _obEventRepository.DeleteForWorkShift(workShift.Id);
                }

                // Skapa och spara OB-händelser baserat på beräknade värden
                if (workShift.EveningHours > 0 && workShift.EveningActiveAtThatTime)
                {
                    var obEvent = new OBEvent
                    {
                        JobProfileId = workShift.JobProfileId,
                        WorkShiftId = workShift.Id,
                        WorkDate = workShift.ShiftDate.Date,
                        StartTime = workShift.EveningStartAtThatTime,
                        EndTime = workShift.NightActiveAtThatTime
                            ? workShift.NightStartAtThatTime
                            : (workShift.EndTime?.TimeOfDay ?? TimeSpan.Zero),
                        Hours = workShift.EveningHours,
                        OBType = LocalizationHelper.Translate("EveningOB"), // "Kväll"
                        RatePerHour = workShift.EveningOBRate,
                        TotalAmount = workShift.EveningOBPay,
                        CreatedAt = DateTime.Now,
                        Notes = string.Format(LocalizationHelper.Translate("EveningOBForDate"), workShift.ShiftDate.ToString("yyyy-MM-dd"))
                    };
                    _obEventRepository.Save(obEvent);
                }
                if (workShift.NightHours > 0 && workShift.NightActiveAtThatTime && workShift.EndTime.HasValue)
                {
                    var obEvent = new OBEvent
                    {
                        JobProfileId = workShift.JobProfileId,
                        WorkShiftId = workShift.Id,
                        WorkDate = workShift.ShiftDate.Date,
                        StartTime = workShift.NightStartAtThatTime,
                        EndTime = workShift.EndTime.Value.TimeOfDay,
                        Hours = workShift.NightHours,
                        OBType = LocalizationHelper.Translate("NightOB"), // "Natt"
                        RatePerHour = workShift.NightOBRate,
                        TotalAmount = workShift.NightOBPay,
                        CreatedAt = DateTime.Now,
                        Notes = string.Format(LocalizationHelper.Translate("NightOBForDate"), workShift.ShiftDate.ToString("yyyy-MM-dd"))
                    };
                    _obEventRepository.Save(obEvent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{LocalizationHelper.Translate("ErrorCreatingOBEvents")}: {ex.Message}");
            }
        }
        #endregion

        #region Public Methods
        public void Reset()
        {
            SelectedDate = DateTime.Today;
            StartTime = new TimeSpan(8, 0, 0);
            EndTime = new TimeSpan(16, 0, 0);
            BreakMinutes = 0;
            Notes = string.Empty;
            IsExtraShift = false;

            CalculateHours();
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}