using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Handlers;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Services.Premium;
using MyWorkSalary.Views.Settings;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels.ShiftTypes
{
    public class RegularShiftViewModel : BaseViewModel
    {
        #region Private Fields
        private readonly HolidayService _holidayService;

        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly IShiftCalculationService _calculationService;
        private readonly IOBEventRepository _obEventRepository;
        private readonly IOBRateRepository _obRateRepository;
        private readonly IOBEventService _obEventService;

        private readonly IPremiumService _premiumService;

        private JobProfile _activeJob;
        private DateTime _selectedDate = DateTime.Today;
        private TimeSpan _startTime = new TimeSpan(8, 0, 0);
        private TimeSpan _endTime = new TimeSpan(16, 0, 0);
        private int _breakMinutes = 0;
        private string _breakSuggestionText = "";
        private string _notes = string.Empty;
        private bool _showCalculation;
        private bool _canSave;
        private decimal _calculatedHours;
        
        #endregion

        #region Constructor
        public RegularShiftViewModel(
            IWorkShiftRepository workShiftRepository,
            IShiftCalculationService calculationService,
            IOBEventRepository obEventRepository,
            IOBRateRepository obRateRepository, 
            IOBEventService oBEventService,
            IPremiumService premiumService,
            HolidayService holidayService)
        {
            _workShiftRepository = workShiftRepository;
            _calculationService = calculationService;
            _obEventRepository = obEventRepository;
            _obRateRepository = obRateRepository;
            _obEventService = oBEventService;

            _premiumService = premiumService;
            _holidayService = holidayService;

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

        #region Day type flags
        private bool _isHoliday;
        private bool _isBigHoliday;
        private bool _isExtraShift = false;
        public bool IsHoliday
        {
            get => _isHoliday;
            set
            {
                if (_isHoliday == value)
                    return;

                _isHoliday = value;
                OnPropertyChanged();

                // Om man stänger av helgdag -> storhelg måste av
                if (!_isHoliday && IsBigHoliday)
                {
                    _isBigHoliday = false;
                    OnPropertyChanged(nameof(IsBigHoliday));
                }
            }
        }
        public bool IsBigHoliday
        {
            get => _isBigHoliday;
            set
            {
                if (_isBigHoliday == value)
                    return;

                _isBigHoliday = value;
                OnPropertyChanged();

                // Om man väljer storhelg -> helgdag måste på
                if (_isBigHoliday && !IsHoliday)
                {
                    _isHoliday = true;
                    OnPropertyChanged(nameof(IsHoliday));
                }
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
        #endregion

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

            var result = _calculationService.CalculateRegularShiftDetailed(
                SelectedDate,
                StartTime,
                EndTime,
                ActiveJob,
                BreakMinutes);

            CalculatedHours = result.TotalHours;

            OnPropertyChanged(nameof(CalculationSummary));
            UpdateBreakSuggestion();

            // Automatisk identifiering av helgdagar
            UpdateHolidayStatus();

            ShowCalculation = result.TotalHours > 0;
            CanSave = ValidateRegularShift();
        }

        /// <summary>
        /// Uppdaterar automatiskt IsHoliday och IsBigHoliday baserat på HolidayService
        /// </summary>
        private void UpdateHolidayStatus()
        {
            if (ActiveJob == null || _holidayService == null)
                return;

            try
            {
                var (isRedDay, isBigHoliday) = _holidayService.GetHolidayStatus(SelectedDate.Date, ActiveJob);

                // Uppdatera bara om värdena har ändrats för att undvika oändliga loopar
                if (_isHoliday != isRedDay)
                {
                    _isHoliday = isRedDay;
                    OnPropertyChanged(nameof(IsHoliday));
                }

                if (_isBigHoliday != isBigHoliday)
                {
                    _isBigHoliday = isBigHoliday;
                    OnPropertyChanged(nameof(IsBigHoliday));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fel vid uppdatering av helgdagsstatus: {ex.Message}");
            }
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
                if (ActiveJob == null)
                    return false;

                // Beräkna detaljerat resultat
                var result = _calculationService.CalculateRegularShiftDetailed(
                    SelectedDate,
                    StartTime,
                    EndTime,
                    ActiveJob,
                    BreakMinutes
                );

                var extraPay = 0m;

                if (IsExtraShift && ActiveJob.ExtraShiftEnabled && ActiveJob.ExtraShiftAmount > 0)
                {
                    if (ActiveJob.ExtraShiftPayType == ExtraShiftPayType.PerHour)
                        extraPay = Math.Round(result.TotalHours * ActiveJob.ExtraShiftAmount, 2);
                    else
                        extraPay = Math.Round(ActiveJob.ExtraShiftAmount, 2);
                }

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

                    IsHoliday = IsHoliday,
                    IsBigHoliday = IsBigHoliday,

                    // Timmar
                    TotalHours = result.TotalHours,
                    RegularHours = result.RegularHours,

                    // Löner
                    RegularPay = result.RegularPay,

                    EveningOBRate = 0,
                    NightOBRate = 0,
                    EveningOBPay = 0,
                    NightOBPay = 0,

                    OBPay = 0,
                    TotalPay = result.RegularPay + extraPay,

                    Notes = Notes,
                    CreatedDate = DateTime.Now,
                    IsExtraShift = IsExtraShift,
                    ExtraShiftPay = extraPay
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
                    var obSummary = _obEventService.RebuildForWorkShift(savedShift);

                    // Endast totalsummor 
                    savedShift.OBHours = obSummary.TotalObHours;
                    savedShift.OBPay = obSummary.TotalObPay;
                    savedShift.TotalPay = savedShift.RegularPay + savedShift.OBPay + savedShift.ExtraShiftPay;

                    // Spara igen så historik/UI stämmer
                    _workShiftRepository.SaveWorkShift(savedShift);
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
        #endregion

        #region Public Methods
        public void Reset()
        {
            SelectedDate = DateTime.Today;
            StartTime = new TimeSpan(8, 0, 0);
            EndTime = new TimeSpan(16, 0, 0);
            BreakMinutes = 0;
            Notes = string.Empty;
            IsHoliday = false;
            IsBigHoliday = false;
            IsExtraShift = false;

            CalculateHours();
        }
        #endregion

        #region Premium Service
        public bool IsPremiumOrSubscriber => _premiumService.IsPremium || _premiumService.IsSubscriber;
        public bool IsFreeUser => !IsPremiumOrSubscriber;

        public ICommand OpenPremiumPageCommand => new Command(async () =>
        {
            await Shell.Current.GoToAsync(nameof(PremiumInfoPage));
        });

        public void RefreshPremiumState()
        {
            OnPropertyChanged(nameof(IsPremiumOrSubscriber));
            OnPropertyChanged(nameof(IsFreeUser));
        }
        #endregion

        #region Debug Property
        // Ändra till true för att visa debug-checkboxar i UI (t.ex. för att testa helgdagar)
        public bool ShowDebugCheckboxes { get; set; } = false;
        #endregion
    }
}