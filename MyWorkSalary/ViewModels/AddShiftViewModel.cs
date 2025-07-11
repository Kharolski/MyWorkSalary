using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Handlers;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.ViewModels.ShiftTypes;
using System.ComponentModel;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public class AddShiftViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly IJobProfileRepository _jobProfileRepository;
        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly ShiftTypeHandler _shiftTypeHandler;
        private readonly IShiftCalculationService _calculationService;

        // Injicerade ViewModels
        public SickLeaveViewModel SickLeaveVM { get; }
        public VABViewModel VABVM { get; }
        public OnCallViewModel OnCallVM { get; }
        public VacationViewModel VacationVM { get; }

        private JobProfile _activeJob;
        private DateTime _selectedDate = DateTime.Today;
        private ShiftType _selectedShiftType = ShiftType.Regular;
        private TimeSpan _startTime = new TimeSpan(8, 0, 0);
        private TimeSpan _endTime = new TimeSpan(16, 0, 0);
        private int _breakMinutes = 0;
        private string _breakSuggestionText = "";
        private string _notes = string.Empty;
        private bool _showCalculation;
        private bool _canSave;
        private decimal _calculatedHours;
        private decimal _calculatedPay;
        #endregion

        #region Constructor
        public AddShiftViewModel(
            IJobProfileRepository jobProfileRepository,
            IWorkShiftRepository workShiftRepository,
            IShiftCalculationService calculationService,
            ShiftTypeHandler shiftTypeHandler,
            SickLeaveViewModel sickLeaveViewModel,
            VABViewModel vabViewModel,
            OnCallViewModel onCallViewModel, 
            VacationViewModel vacationViewModel)
        {
            _jobProfileRepository = jobProfileRepository;
            _workShiftRepository = workShiftRepository;
            _calculationService = calculationService;
            _shiftTypeHandler = shiftTypeHandler;
            SickLeaveVM = sickLeaveViewModel;
            VABVM = vabViewModel;
            OnCallVM = onCallViewModel;
            VacationVM = vacationViewModel;

            // Prenumerera på ValidationChanged events
            SickLeaveVM.ValidationChanged += () => ValidateAndUpdateCanSave();
            VABVM.ValidationChanged += () => ValidateAndUpdateCanSave();
            OnCallVM.ValidationChanged += () => ValidateAndUpdateCanSave();
            VacationVM.ValidationChanged += () => ValidateAndUpdateCanSave();

            // Commands
            SaveCommand = new Command(OnSave, CanExecuteSave);
            CancelCommand = new Command(OnCancel);

            LoadActiveJob();
            CalculateHours();
        }
        #endregion

        #region Properties
        #region Visibility Properties
        // Sjukskrivning
        public bool ShowSickLeaveForm => SelectedShiftType == ShiftType.SickLeave;

        // VAB
        public bool ShowVABForm => SelectedShiftType == ShiftType.VAB;

        // OnCall
        public bool ShowOnCallForm => SelectedShiftType == ShiftType.OnCall;

        // Vacation 
        public bool ShowVacationForm => SelectedShiftType == ShiftType.Vacation;

        // Vanliga fält (Regular)
        public bool ShowGeneralForm => SelectedShiftType == ShiftType.Regular;
        public bool ShowTimeFields => ShowGeneralForm;
        public bool ShowBreakField => ShowGeneralForm;
        public bool ShowGeneralNotes => ShowGeneralForm;
        public bool ShowGeneralCalculation => ShowGeneralForm && ShowCalculation;
        #endregion

        public JobProfile ActiveJob
        {
            get => _activeJob;
            set
            {
                _activeJob = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActiveJobTitle));

                // Uppdatera child ViewModels
                SickLeaveVM.UpdateContext(SelectedDate, _activeJob);
                VABVM.UpdateContext(SelectedDate, _activeJob);
                OnCallVM.UpdateContext(SelectedDate, _activeJob);
                VacationVM.UpdateContext(SelectedDate, _activeJob);
                CalculateHours();
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

                // Uppdatera child ViewModels
                SickLeaveVM.UpdateContext(_selectedDate, ActiveJob);
                VABVM.UpdateContext(_selectedDate, ActiveJob);
                OnCallVM.UpdateContext(_selectedDate, ActiveJob);     
                VacationVM.UpdateContext(_selectedDate, ActiveJob);
                CalculateHours();
            }
        }

        // Shift Type
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

        private string _selectedShiftTypeDisplay = "Vanligt pass";
        public List<string> ShiftTypeDisplayNames { get; } = new List<string>
        {
            "Vanligt pass",      // Regular
            "Jour",              // OnCall
            "Sjukskrivning",     // SickLeave
            "Semester",          // Vacation
            "Vård av barn"       // VAB
        };

        public string SelectedShiftTypeDisplay
        {
            get => _selectedShiftTypeDisplay;
            set
            {
                _selectedShiftTypeDisplay = value;
                _selectedShiftType = GetShiftTypeFromDisplay(value);
                OnPropertyChanged();
                OnSelectedShiftTypeChanged();
            }
        }

        // Time Fields (bara för Regular/OnCall)
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

        // Calculations (bara för Regular/OnCall)
        public string CalculationSummary
        {
            get
            {
                if (SelectedShiftTypeDisplay == "Semester")
                {
                    return "Semester: 1 dag";
                }

                // Vanliga pass - visa rast-info
                if (ShowBreakField && BreakMinutes > 0)
                {
                    return $"Arbetstid: {CalculatedHours:F1}h (efter {BreakMinutes} min rast)";
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

        public decimal CalculatedHours
        {
            get => _calculatedHours;
            set
            {
                _calculatedHours = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CalculationSummary));
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
        public ICommand CancelCommand { get; }
        #endregion

        #region Initialization
        private void LoadActiveJob()
        {
            try
            {
                ActiveJob = _jobProfileRepository.GetActiveJob();
                if (ActiveJob == null)
                {
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
                // Bara beräkna för Regular
                if (!ShowGeneralForm)
                {
                    CalculatedHours = 0;
                    CalculatedPay = 0;
                    ShowCalculation = false;
                    ValidateAndUpdateCanSave();
                    return;
                }

                var breakToUse = ShowBreakField ? BreakMinutes : 0;
                var result = _calculationService.CalculateShiftHoursAndPay(
                    SelectedDate, StartTime, EndTime,
                    SelectedShiftType, 1, ActiveJob, breakToUse);

                CalculatedHours = result.Hours;
                CalculatedPay = result.Pay;

                // Uppdatera rast-förslag
                if (ShowTimeFields && ShowBreakField)
                {
                    UpdateBreakSuggestion();
                }

                ShowCalculation = CalculatedHours > 0;
                ValidateAndUpdateCanSave();
                OnPropertyChanged(nameof(ShowPay));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fel i CalculateHours: {ex.Message}");
                ShowCalculation = false;
                CanSave = false;
            }
        }

        private void ValidateAndUpdateCanSave()
        {
            if (ActiveJob == null)
            {
                CanSave = false;
                return;
            }

            var canSaveResult = SelectedShiftType switch
            {
                ShiftType.SickLeave => SickLeaveVM.CanSave(),
                ShiftType.VAB => VABVM.CanSave(),
                ShiftType.OnCall => OnCallVM.CanSave(),
                ShiftType.Vacation => VacationVM.CanSave(),
                _ => ValidateRegularShift()
            };

            CanSave = canSaveResult;
        }

        private bool ValidateRegularShift()
        {
            if (!ShowTimeFields)
                return true;

            var totalHours = CalculatedHours + (BreakMinutes / 60m);
            return _calculationService.ValidateHours(CalculatedHours) &&
                   _calculationService.ValidateBreakMinutes(BreakMinutes, totalHours);
        }

        private bool CanExecuteSave() => CanSave;
        #endregion

        #region Save Actions
        private async void OnSave()
        {
            if (ActiveJob == null)
            {
                await Shell.Current.DisplayAlert("Fel", "Inget aktivt jobb.", "OK");
                return;
            }

            try
            {
                var success = SelectedShiftType switch
                {
                    ShiftType.SickLeave => await HandleSickLeaveSave(),
                    ShiftType.VAB => await HandleVABSave(),
                    ShiftType.Regular => await HandleRegularShiftSave(),
                    ShiftType.OnCall => await HandleOnCallShiftSave(),
                    ShiftType.Vacation => await HandleVacationSave()
                };

                if (success)
                {
                    await Shell.Current.GoToAsync("..");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Fel", $"Kunde inte spara: {ex.Message}", "OK");
            }
        }

        private async Task<bool> HandleSickLeaveSave()
        {
            var success = await SickLeaveVM.SaveSickLeave();
            if (success)
            {
                await Shell.Current.DisplayAlert("✅ Sparat!", "Sjukdag registrerad", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("❌ Fel", "Kunde inte spara sjukdag", "OK");
            }
            return success;
        }

        private async Task<bool> HandleVABSave()
        {
            if (VABVM == null)
            {
                await Shell.Current.DisplayAlert("❌ Fel", "VAB ViewModel inte initialiserad", "OK");
                return false;
            }

            try
            {
                var success = await VABVM.SaveVAB();
                if (success)
                {
                    await Shell.Current.DisplayAlert("✅ Sparat!", "VAB-dag registrerad", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("❌ Fel", $"Kunde inte spara VAB-dag: {VABVM.ValidationMessage}", "OK");
                }
                return success;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("❌ Fel", $"Kunde inte spara VAB-dag: {ex.Message}", "OK");
                return false;
            }
        }

        // Hantera Regular shifts
        private async Task<bool> HandleRegularShiftSave()
        {
            try
            {
                var workShift = new WorkShift
                {
                    JobProfileId = ActiveJob.Id,
                    ShiftDate = SelectedDate,
                    ShiftType = ShiftType.Regular,
                    StartTime = DateTime.Today.Add(StartTime),
                    EndTime = DateTime.Today.Add(EndTime),
                    BreakMinutes = BreakMinutes,
                    TotalHours = CalculatedHours,
                    TotalPay = CalculatedPay,
                    Notes = Notes,
                    CreatedDate = DateTime.Now
                };

                var savedShift = _workShiftRepository.SaveWorkShift(workShift);

                if (savedShift != null && savedShift.Id > 0)
                {
                    await Shell.Current.DisplayAlert("✅ Sparat!", "Vanligt pass registrerat", "OK");
                    return true;
                }
                else
                {
                    await Shell.Current.DisplayAlert("❌ Fel", "Kunde inte spara passet", "OK");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("❌ Fel", $"Kunde inte spara passet: {ex.Message}", "OK");
                return false;
            }
        }

        // Hantera OnCall shifts
        private async Task<bool> HandleOnCallShiftSave()
        {
            if (OnCallVM == null)
            {
                await Shell.Current.DisplayAlert("❌ Fel", "OnCall ViewModel inte initialiserad", "OK");
                return false;
            }

            try
            {
                var success = await OnCallVM.SaveOnCall();
                if (success)
                {
                    await Shell.Current.DisplayAlert("✅ Sparat!", "Jourpass registrerat", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("❌ Fel", $"Kunde inte spara jourpass: {OnCallVM.ValidationMessage}", "OK");
                }
                return success;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("❌ Fel", $"Kunde inte spara jourpass: {ex.Message}", "OK");
                return false;
            }
        }

        // Handle Vacation shifts
        private async Task<bool> HandleVacationSave()
        {
            if (VacationVM == null)
            {
                await Shell.Current.DisplayAlert("❌ Fel", "Vacation ViewModel inte initialiserad", "OK");
                return false;
            }

            try
            {
                var success = await VacationVM.SaveVacation();
                if (success)
                {
                    await Shell.Current.DisplayAlert("✅ Sparat!", "Semester registrerad", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("❌ Fel", $"Kunde inte spara semester: {VacationVM.ValidationMessage}", "OK");
                }
                return success;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("❌ Fel", $"Kunde inte spara semester: {ex.Message}", "OK");
                return false;
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

        #region Helper Methods
        private ShiftType GetShiftTypeFromDisplay(string displayName)
        {
            return displayName switch
            {
                "Vanligt pass" => ShiftType.Regular,
                "Jour" => ShiftType.OnCall,
                "Sjukskrivning" => ShiftType.SickLeave,
                "Semester" => ShiftType.Vacation,
                "Vård av barn" => ShiftType.VAB,
                _ => ShiftType.Regular
            };
        }

        private void UpdateBreakSuggestion()
        {
            if (!ShowTimeFields || !ShowBreakField)
            {
                BreakSuggestionText = "";
                return;
            }

            var totalHours = (EndTime - StartTime).TotalHours;
            if (EndTime < StartTime)
                totalHours += 24; // Pass över midnatt

            // Använd service för att få förslag
            BreakSuggestionText = _calculationService.GetBreakSuggestionText((decimal)totalHours);

            // Auto-föreslå rast om användaren inte satt någon
            if (BreakMinutes == 0)
            {
                var suggestedMinutes = _calculationService.SuggestBreakMinutes((decimal)totalHours);
                if (suggestedMinutes > 0)
                {
                    BreakMinutes = suggestedMinutes;
                }
            }
        }

        private void OnSelectedShiftTypeChanged()
        {
            // Trigga alla visibility properties
            OnPropertyChanged(nameof(ShowGeneralForm));
            OnPropertyChanged(nameof(ShowTimeFields));
            OnPropertyChanged(nameof(ShowBreakField));
            OnPropertyChanged(nameof(ShowGeneralNotes));
            OnPropertyChanged(nameof(ShowGeneralCalculation));
            OnPropertyChanged(nameof(ShowSickLeaveForm));
            OnPropertyChanged(nameof(ShowVABForm));
            OnPropertyChanged(nameof(ShowOnCallForm));
            OnPropertyChanged(nameof(ShowVacationForm));
            OnPropertyChanged(nameof(CalculationSummary));

            // Uppdatera context för child ViewModels
            if (SelectedShiftType == ShiftType.SickLeave)
            {
                SickLeaveVM.UpdateContext(SelectedDate, ActiveJob);
            }
            else if (SelectedShiftType == ShiftType.VAB)
            {
                VABVM.UpdateContext(SelectedDate, ActiveJob);
            }
            else if (SelectedShiftType == ShiftType.OnCall) 
            {
                OnCallVM.UpdateContext(SelectedDate, ActiveJob);
            }
            else if (SelectedShiftType == ShiftType.Vacation)   
            {
                VacationVM.UpdateContext(SelectedDate, ActiveJob);
            }

            CalculateHours();
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

