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

        private readonly SickLeaveHandler _sickLeaveHandler;

        // Injicerade ViewModels
        public SickLeaveViewModel SickLeaveVM { get; }
        public VABViewModel VABVM { get; }
        public OnCallViewModel OnCallVM { get; }
        public VacationViewModel VacationVM { get; }
        public RegularShiftViewModel RegularShiftVM { get; }

        private JobProfile _activeJob;
        private DateTime _selectedDate = DateTime.Today;
        private ShiftType _selectedShiftType = ShiftType.Regular;
        private bool _canSave;
        #endregion

        #region Constructor
        public AddShiftViewModel(
            IJobProfileRepository jobProfileRepository,
            IWorkShiftRepository workShiftRepository,
            IShiftCalculationService calculationService,
            ShiftTypeHandler shiftTypeHandler,
            SickLeaveHandler sickLeaveHandler,
            SickLeaveViewModel sickLeaveViewModel,
            VABViewModel vabViewModel,
            OnCallViewModel onCallViewModel, 
            VacationViewModel vacationViewModel)
        {
            _jobProfileRepository = jobProfileRepository;
            _workShiftRepository = workShiftRepository;
            _sickLeaveHandler = sickLeaveHandler;
            SickLeaveVM = sickLeaveViewModel;
            VABVM = vabViewModel;
            OnCallVM = onCallViewModel;
            VacationVM = vacationViewModel;

            RegularShiftVM = new RegularShiftViewModel(workShiftRepository, calculationService);

            // Prenumerera på ValidationChanged events
            SickLeaveVM.ValidationChanged += () => ValidateAndUpdateCanSave();
            VABVM.ValidationChanged += () => ValidateAndUpdateCanSave();
            OnCallVM.ValidationChanged += () => ValidateAndUpdateCanSave();
            VacationVM.ValidationChanged += () => ValidateAndUpdateCanSave();

            // Commands
            SaveCommand = new Command(OnSave, CanExecuteSave);
            CancelCommand = new Command(OnCancel);

            LoadActiveJob();
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
        public bool ShowGeneralCalculation => ShowGeneralForm && RegularShiftVM.ShowCalculation;
        #endregion

        public JobProfile ActiveJob
        {
            get => _activeJob;
            set
            {
                _activeJob = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActiveJobTitle));

                // Synka till RegularShiftVM
                RegularShiftVM.ActiveJob = _activeJob;

                // Uppdatera child ViewModels
                SickLeaveVM.UpdateContext(SelectedDate, _activeJob);
                VABVM.UpdateContext(SelectedDate, _activeJob);
                OnCallVM.UpdateContext(SelectedDate, _activeJob);
                VacationVM.UpdateContext(SelectedDate, _activeJob);
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

                // Synka till RegularShiftVM
                RegularShiftVM.SelectedDate = _selectedDate;

                // Uppdatera child ViewModels
                SickLeaveVM.UpdateContext(_selectedDate, ActiveJob);
                VABVM.UpdateContext(_selectedDate, ActiveJob);
                OnCallVM.UpdateContext(_selectedDate, ActiveJob);     
                VacationVM.UpdateContext(_selectedDate, ActiveJob);
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

        public string CalculationSummary => RegularShiftVM.CalculationSummary;

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
        private void ValidateAndUpdateCanSave()
        {
            if (ActiveJob == null)
            {
                _canSave = false;
                return;
            }

            var canSaveResult = SelectedShiftType switch
            {
                ShiftType.SickLeave => SickLeaveVM.CanSave(),
                ShiftType.VAB => VABVM.CanSave(),
                ShiftType.OnCall => OnCallVM.CanSave(),
                ShiftType.Vacation => VacationVM.CanSave(),
                ShiftType.Regular => RegularShiftVM.CanSave,
                _ => false
            };

            _canSave = canSaveResult;
            ((Command)SaveCommand).ChangeCanExecute();
        }

        private bool CanExecuteSave()
        {
            return SelectedShiftType switch
            {
                ShiftType.Regular => RegularShiftVM.CanSave,
                ShiftType.SickLeave => SickLeaveVM.CanSave(),
                ShiftType.VAB => VABVM.CanSave(),
                ShiftType.OnCall => OnCallVM.CanSave(),
                ShiftType.Vacation => VacationVM.CanSave(),
                _ => false
            };
        }
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
                    ShiftType.Vacation => await HandleVacationSave(),
                    _ => false
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

        private async Task<bool> HandleRegularShiftSave()
        {
            // Kontrollera konflikter först
            if (!await CheckForConflictsBeforeSave(ShiftType.Regular))
                return false;

            var success = await RegularShiftVM.SaveRegularShift();
            if (success)
                await Shell.Current.DisplayAlert("✅ Sparat!", "Vanligt pass registrerat", "OK");
            else
                await Shell.Current.DisplayAlert("❌ Fel", "Kunde inte spara passet", "OK");
            return success;
        }

        private async Task<bool> HandleSickLeaveSave()
        {
            // Kontrollera konflikter FÖRST
            if (!await CheckForConflictsBeforeSave(ShiftType.SickLeave))
            {
                return false; // Konflikt hittad, avbryt sparande
            }

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
            // Kontrollera konflikter FÖRST
            if (!await CheckForConflictsBeforeSave(ShiftType.VAB))
            {
                return false; // Konflikt hittad, avbryt sparande
            }

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
                    if (!string.IsNullOrEmpty(VABVM.ValidationMessage))
                    {
                        await Shell.Current.DisplayAlert("❌ Fel", $"Kunde inte spara VAB-dag: {VABVM.ValidationMessage}", "OK");
                    }
                    // Om ValidationMessage är tom = användaren avbröt, visa inget meddelande
                }
                return success;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("❌ Fel", $"Kunde inte spara VAB-dag: {ex.Message}", "OK");
                return false;
            }
        }

        // Hantera OnCall shifts
        private async Task<bool> HandleOnCallShiftSave()
        {
            if (!await CheckForConflictsBeforeSave(ShiftType.OnCall))
            {
                return false;
            }

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
            if (!await CheckForConflictsBeforeSave(ShiftType.Vacation))
            {
                return false;
            }

            if (VacationVM == null)
            {
                await Shell.Current.DisplayAlert("❌ Fel", "Vacation ViewModel inte initialiserad", "OK");
                return false;
            }

            try
            {
                // Hantera den nya return-typen (bool, string)
                var (success, message) = await VacationVM.SaveVacation();

                if (success)
                {
                    await Shell.Current.DisplayAlert("✅ Sparat!", message, "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("❌ Fel", message, "OK");
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
        }

        /// <summary>
        /// Kontrollerar konflikter innan sparande
        /// </summary>
        private async Task<bool> CheckForConflictsBeforeSave(ShiftType newShiftType)
        {
            try
            {
                // Använd SickLeaveHandler's konflikt-kontroll (den fungerar för alla typer)
                var conflictResult = _sickLeaveHandler.CheckForConflicts(SelectedDate, ActiveJob.Id, newShiftType);

                if (!conflictResult.CanProceed)
                {
                    // Visa felmeddelande
                    await Shell.Current.DisplayAlert("❌ Konflikt", conflictResult.ErrorMessage, "OK");
                    return false;
                }

                // ✨ NY: Hantera bekräftelse för ersättning
                if (conflictResult.RequiresConfirmation)
                {
                    bool userConfirmed = await Shell.Current.DisplayAlert(
                        "Ersätt pass?",
                        conflictResult.ConfirmationMessage,
                        "Ja, ersätt",
                        "Avbryt");

                    if (!userConfirmed)
                    {
                        return false; // Användaren avbröt
                    }

                    // Ta bort befintligt pass
                    await DeleteExistingShift();
                }

                return true;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("❌ Fel", $"Kunde inte kontrollera konflikter: {ex.Message}", "OK");
                return false;
            }
        }

        /// <summary>
        /// Tar bort befintligt pass för valt datum
        /// </summary>
        private async Task DeleteExistingShift()
        {
            try
            {
                var existingShifts = _workShiftRepository.GetWorkShiftsForDate(ActiveJob.Id, SelectedDate);
                if (existingShifts.Any())
                {
                    var shiftToDelete = existingShifts.First();
                    _workShiftRepository.DeleteWorkShift(shiftToDelete.Id);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("❌ Fel", $"Kunde inte ta bort befintligt pass: {ex.Message}", "OK");
                throw; // Re-throw så att sparandet avbryts
            }
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

