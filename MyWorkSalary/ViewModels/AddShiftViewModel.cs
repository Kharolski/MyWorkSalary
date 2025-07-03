using MyWorkSalary.Models;
using MyWorkSalary.Services;
using MyWorkSalary.Services.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public class AddShiftViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly DatabaseService _databaseService;
        private readonly IConflictResolutionService _conflictResolutionService;
        private readonly IWorkShiftService _workShiftService;
        private readonly IShiftCalculationService _calculationService;
        private readonly IShiftBuilderService _shiftBuilderService;
        private readonly IConflictHandlerService _conflictHandlerService;

        private JobProfile _activeJob;
        private DateTime _selectedDate = DateTime.Today;
        private TimeSpan _startTime = new TimeSpan(8, 0, 0); // 08:00
        private TimeSpan _endTime = new TimeSpan(16, 0, 0);  // 16:00
        private ShiftType _selectedShiftType = ShiftType.Regular;
        private string _notes = string.Empty;
        private bool _showCalculation;
        private bool _canSave;
        private decimal _calculatedHours;
        private decimal _calculatedPay;
        #endregion

        #region Constructor
        public AddShiftViewModel(DatabaseService databaseService, 
            IConflictResolutionService conflictResolutionService, 
            IWorkShiftService workShiftService, 
            IShiftCalculationService calculationService, 
            IShiftBuilderService shiftBuilderService, 
            IConflictHandlerService conflictHandlerService)
        {
            _databaseService = databaseService;
            _conflictResolutionService = conflictResolutionService;
            _workShiftService = workShiftService;
            _calculationService = calculationService;
            _shiftBuilderService = shiftBuilderService;
            _conflictHandlerService = conflictHandlerService;

            // Commands
            SaveCommand = new Command(OnSave, CanExecuteSave);
            CancelCommand = new Command(OnCancel);

            // Ladda aktivt jobb
            LoadActiveJob();

            // Beräkna initial
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
                OnPropertyChanged(nameof(ActiveJobTitle));
            }
        }

        public string ActiveJobTitle => ActiveJob?.JobTitle ?? "Inget aktivt jobb";

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

        public ShiftType SelectedShiftType
        {
            get => _selectedShiftType;
            set
            {
                _selectedShiftType = value;
                OnPropertyChanged();
                CalculateHours();
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

        public ObservableCollection<ShiftType> ShiftTypes { get; }

        public List<string> ShiftTypeDisplayNames { get; } = new List<string>
        {
            "Vanligt pass",      // Regular
            "Övertid",           // Overtime  
            "Jour",              // OnCall
            "Sjukskrivning",     // SickLeave
            "Semester",          // Vacation
            "Utbildning"         // Training
        };

        private string _selectedShiftTypeDisplay = "Vanligt pass";
        public string SelectedShiftTypeDisplay
        {
            get => _selectedShiftTypeDisplay;
            set
            {
                _selectedShiftTypeDisplay = value;
                _selectedShiftType = GetShiftTypeFromDisplay(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowTimeFields));
                OnPropertyChanged(nameof(ShowDaysField));
                OnPropertyChanged(nameof(CalculationSummary));
                CalculateHours();
            }
        }

        public bool ShowTimeFields =>
            SelectedShiftTypeDisplay != "Sjukskrivning" && SelectedShiftTypeDisplay != "Semester";

        public bool ShowDaysField =>
            SelectedShiftTypeDisplay == "Sjukskrivning" || SelectedShiftTypeDisplay == "Semester";

        private string _numberOfDays = "1";
        public string NumberOfDays
        {
            get => _numberOfDays;
            set
            {
                _numberOfDays = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CalculationSummary));
            }
        }

        public string CalculationSummary
        {
            get
            {
                if (ShowDaysField)
                {
                    return $"Period: {NumberOfDays} dagar";
                }
                return $"Totalt: {CalculatedHours:F1} timmar";
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

        public decimal CalculatedHours
        {
            get => _calculatedHours;
            set
            {
                _calculatedHours = value;
                OnPropertyChanged();
            }
        }

        public decimal CalculatedPay
        {
            get => _calculatedPay;
            set
            {
                _calculatedPay = value;
                OnPropertyChanged();
            }
        }

        public bool ShowPay => ActiveJob?.HourlyRate > 0 && CalculatedPay > 0;

        //public string JobId { get; set; }
        #endregion

        #region Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        #region Initialization & Setup
        private void LoadActiveJob()
        {
            try
            {
                var jobs = _databaseService.GetJobProfiles();
                ActiveJob = jobs.FirstOrDefault(j => j.IsActive);

                if (ActiveJob == null)
                {
                    // Ingen aktivt jobb - detta borde inte hända
                    Shell.Current.DisplayAlert("Fel", "Inget aktivt jobb hittades.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fel i LoadActiveJob: {ex.Message}");
            }
        }

        #endregion

        #region Calculations & Validation
        private void CalculateHours()
        {
            try
            {
                int days = ShowDaysField && int.TryParse(NumberOfDays, out int d) ? d : 1;

                var result = _calculationService.CalculateShiftHoursAndPay(
                    SelectedDate, StartTime, EndTime,
                    GetShiftTypeFromDisplay(SelectedShiftTypeDisplay),
                    days, ActiveJob);

                CalculatedHours = result.Hours;
                CalculatedPay = result.Pay;

                // Visa beräkning
                ShowCalculation = CalculatedHours > 0 || ShowDaysField;

                // Validering
                if (ShowDaysField)
                {
                    CanSave = days > 0 && ActiveJob != null;
                }
                else
                {
                    CanSave = _calculationService.ValidateHours(CalculatedHours) && ActiveJob != null;
                }

                OnPropertyChanged(nameof(ShowPay));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fel i CalculateHours: {ex.Message}");
                ShowCalculation = false;
                CanSave = false;
            }
        }
        
        private bool CanExecuteSave()
        {
            return CanSave;
        }

        #endregion

        #region Main Actions
        private async void OnSave()
        {
            if (ActiveJob == null)
            {
                await Shell.Current.DisplayAlert("Fel", "Inget aktivt jobb.", "OK");
                return;
            }

            try
            {
                WorkShift workShift;
                if (ShowDaysField) // Semester eller Sjukskrivning
                {
                    if (!int.TryParse(NumberOfDays, out int days) || days <= 0)
                    {
                        await Shell.Current.DisplayAlert("Fel", "Ange ett giltigt antal dagar.", "OK");
                        return;
                    }
                    workShift = _shiftBuilderService.BuildLeaveShift(
                        ActiveJob, SelectedDate, GetShiftTypeFromDisplay(SelectedShiftTypeDisplay),
                        days, CalculatedPay, Notes);
                }
                else // Vanliga arbetspass
                {
                    workShift = _shiftBuilderService.BuildRegularShift(
                        ActiveJob, SelectedDate, StartTime, EndTime,
                        GetShiftTypeFromDisplay(SelectedShiftTypeDisplay),
                        CalculatedHours, CalculatedPay, Notes);
                }

                // Spara med konflikthantering
                var result = await _workShiftService.SaveWorkShiftWithValidation(workShift);

                // Hantera specialmeddelande
                if (!result.Success && result.Message.StartsWith("CONFLICT_RESOLUTION_NEEDED|"))
                {
                    await _conflictHandlerService.HandleSickLeaveConflict(result.Message, workShift);
                    return;
                }

                if (!result.Success && result.Message.StartsWith("SICK_CONFLICT|"))
                {
                    await _conflictHandlerService.HandleWorkShiftSickConflict(result.Message, workShift, ActiveJob);
                    return;
                }

                // Hantera arbetspass konflikt
                if (!result.Success && result.Message.StartsWith("WORK_CONFLICT|"))
                {
                    await _conflictHandlerService.HandleWorkShiftConflict(result.Message, workShift);
                    return;
                }

                // Hantera sammanslagning av perioder
                if (!result.Success && result.Message.StartsWith("MERGE_PERIODS|"))
                {
                    await _conflictHandlerService.HandlePeriodMerging(result.Message, workShift);
                    return;
                }

                if (result.Success)
                {
                    await Shell.Current.DisplayAlert("Sparat!", result.Message, "OK");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.DisplayAlert("⚠️ Fel", result.Message, "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fel", $"Kunde inte spara: {ex.Message}", "OK");
            }
        }

        private async void OnCancel()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Avbryt",
                "Vill du avbryta utan att spara?",
                "Ja, avbryt",
                "Fortsätt redigera");

            if (confirm)
            {
                await Shell.Current.GoToAsync("..");
            }
        }

        #endregion

        #region Conflict Handlers
        // Flyttat till Services/Interfaces/
        #endregion

        #region Helper Methods
        private ShiftType GetShiftTypeFromDisplay(string displayName)
        {
            return displayName switch
            {
                "Vanligt pass" => ShiftType.Regular,
                "Övertid" => ShiftType.Overtime,
                "Jour" => ShiftType.OnCall,
                "Sjukskrivning" => ShiftType.SickLeave,
                "Semester" => ShiftType.Vacation,
                "Utbildning" => ShiftType.Training,
                _ => ShiftType.Regular
            };
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