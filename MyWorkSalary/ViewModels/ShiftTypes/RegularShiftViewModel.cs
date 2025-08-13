using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;
using System.ComponentModel;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels.ShiftTypes
{
    public class RegularShiftViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly IShiftCalculationService _calculationService;

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
            IShiftCalculationService calculationService)
        {
            _workShiftRepository = workShiftRepository;
            _calculationService = calculationService;

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
                    return $"Arbetstid: {CalculatedHours:F1}t (efter {BreakMinutes} min rast)";
                }
                return $"Totalt: {CalculatedHours:F1} timmar";
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

            var result = _calculationService.CalculateShiftHoursAndPay(
                SelectedDate, StartTime, EndTime,
                ShiftType.Regular, 1, ActiveJob, BreakMinutes);

            CalculatedHours = result.Hours;

            UpdateBreakSuggestion();

            ShowCalculation = CalculatedHours > 0;
            CanSave = ValidateRegularShift();
        }

        private bool ValidateRegularShift()
        {
            var totalHours = CalculatedHours + (BreakMinutes / 60m);
            return _calculationService.ValidateHours(CalculatedHours) &&
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
            if (ActiveJob == null)
                return false;

            var workShift = new WorkShift
            {
                JobProfileId = ActiveJob.Id,
                ShiftDate = SelectedDate,
                ShiftType = ShiftType.Regular,
                StartTime = SelectedDate.Date.Add(StartTime),
                EndTime = SelectedDate.Date.Add(EndTime),
                BreakMinutes = BreakMinutes,
                TotalHours = CalculatedHours,
                Notes = Notes,
                CreatedDate = DateTime.Now,
                IsExtraShift = IsExtraShift
            };

            var savedShift = _workShiftRepository.SaveWorkShift(workShift);

            return savedShift != null && savedShift.Id > 0;
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