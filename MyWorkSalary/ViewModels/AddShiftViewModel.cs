using MyWorkSalary.Helpers.Localization;
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

        public string ActiveJobTitle => ActiveJob?.JobTitle ?? LocalizationHelper.Translate("RegularShift_Validation_NoJobSelected");

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

        private string _selectedShiftTypeDisplay = LocalizationHelper.Translate("ShiftType_Regular");
        public List<string> ShiftTypeDisplayNames { get; } = new List<string>
        {
            LocalizationHelper.Translate("ShiftType_Add_Regular"),  // Regular
            LocalizationHelper.Translate("ShiftType_SickLeave"),    // SickLeave
            LocalizationHelper.Translate("ShiftType_Add_VAB"),      // VAB
            LocalizationHelper.Translate("ShiftType_Vacation"),     // Vacation
            LocalizationHelper.Translate("ShiftType_OnCall")        // OnCall
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
                    Shell.Current.DisplayAlert(
                        LocalizationHelper.Translate("Error"),
                        LocalizationHelper.Translate("NoActiveJobFound"),
                        LocalizationHelper.Translate("OK"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadActiveJob: {ex.Message}");
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
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("Error"),
                    LocalizationHelper.Translate("NoActiveJob"),
                    LocalizationHelper.Translate("OK"));
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
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("Error"),
                    string.Format(LocalizationHelper.Translate("SaveError_Generic"), ex.Message),
                    LocalizationHelper.Translate("OK"));
            }
        }

        private async Task<bool> HandleRegularShiftSave()
        {
            // Kontrollera konflikter först
            if (!await CheckForConflictsBeforeSave(ShiftType.Regular))
                return false;

            var success = await RegularShiftVM.SaveRegularShift();
            if (success)
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("SaveSuccess"),
                    LocalizationHelper.Translate("SaveSuccess_RegularShift"),
                    LocalizationHelper.Translate("OK"));
            else
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("Error"),
                    LocalizationHelper.Translate("SaveError_RegularShift"),
                    LocalizationHelper.Translate("OK"));
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
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("SaveSuccess"),
                    LocalizationHelper.Translate("SaveSuccess_SickLeave"),
                    LocalizationHelper.Translate("OK"));
            }
            else
            {
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("Error"),
                    LocalizationHelper.Translate("SaveError_SickLeave"),
                    LocalizationHelper.Translate("OK"));
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
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("Error"),
                    string.Format(LocalizationHelper.Translate("ViewModelNotInitialized"), "VAB"),
                    LocalizationHelper.Translate("OK"));
                return false;
            }

            try
            {
                var success = await VABVM.SaveVAB();
                if (success)
                {
                    await Shell.Current.DisplayAlert(
                        LocalizationHelper.Translate("SaveSuccess"),
                        LocalizationHelper.Translate("SaveSuccess_VAB"),
                        LocalizationHelper.Translate("OK"));
                }
                else
                {
                    if (!string.IsNullOrEmpty(VABVM.ValidationMessage))
                    {
                        await Shell.Current.DisplayAlert(
                            LocalizationHelper.Translate("Error"),
                            string.Format(LocalizationHelper.Translate("SaveError_VAB"), VABVM.ValidationMessage),
                            LocalizationHelper.Translate("OK"));
                    }
                    // Om ValidationMessage är tom = användaren avbröt, visa inget meddelande
                }
                return success;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("Error"),
                    string.Format(LocalizationHelper.Translate("SaveError_VAB"), ex.Message),
                    LocalizationHelper.Translate("OK"));
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
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("Error"),
                    string.Format(LocalizationHelper.Translate("ViewModelNotInitialized"), "OnCall"),
                    LocalizationHelper.Translate("OK"));
                return false;
            }

            try
            {
                var success = await OnCallVM.SaveOnCall();
                if (success)
                {
                    await Shell.Current.DisplayAlert(
                        LocalizationHelper.Translate("SaveSuccess"),
                        LocalizationHelper.Translate("SaveSuccess_OnCall"),
                        LocalizationHelper.Translate("OK"));
                }
                else
                {
                    await Shell.Current.DisplayAlert(
                        LocalizationHelper.Translate("Error"),
                        string.Format(LocalizationHelper.Translate("SaveError_OnCall"),
                            OnCallVM.ValidationMessage ?? string.Empty),
                        LocalizationHelper.Translate("OK"));
                }
                return success;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("Error"),
                    string.Format(LocalizationHelper.Translate("SaveError_OnCall"), ex.Message),
                    LocalizationHelper.Translate("OK"));
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
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("Error"),
                    string.Format(LocalizationHelper.Translate("ViewModelNotInitialized"), "Vacation"),
                    LocalizationHelper.Translate("OK"));
                return false;
            }

            try
            {
                // Hantera den nya return-typen (bool, string)
                var (success, message) = await VacationVM.SaveVacation();

                if (success)
                {
                    await Shell.Current.DisplayAlert(
                        LocalizationHelper.Translate("SaveSuccess"),
                        message, // The message is already localized from VacationVM
                        LocalizationHelper.Translate("OK"));
                }
                else
                {
                    await Shell.Current.DisplayAlert(
                        LocalizationHelper.Translate("Error"),
                        message, // The message is already localized from VacationVM
                        LocalizationHelper.Translate("OK"));
                }

                return success;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("Error"),
                    string.Format(LocalizationHelper.Translate("SaveError_Vacation"), ex.Message),
                    LocalizationHelper.Translate("OK"));
                return false;
            }
        }

        private async void OnCancel()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                LocalizationHelper.Translate("CancelDialog_Title"),
                LocalizationHelper.Translate("CancelDialog_Message"),
                LocalizationHelper.Translate("CancelDialog_Confirm"),
                LocalizationHelper.Translate("CancelDialog_Decline"));

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
                _ when displayName == LocalizationHelper.Translate("ShiftType_Add_Regular") => ShiftType.Regular,
                _ when displayName == LocalizationHelper.Translate("ShiftType_OnCall") => ShiftType.OnCall,
                _ when displayName == LocalizationHelper.Translate("ShiftType_SickLeave") => ShiftType.SickLeave,
                _ when displayName == LocalizationHelper.Translate("ShiftType_Vacation") => ShiftType.Vacation,
                _ when displayName == LocalizationHelper.Translate("ShiftType_Add_VAB") => ShiftType.VAB,
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
                    await Shell.Current.DisplayAlert(
                        LocalizationHelper.Translate("Error"),
                        conflictResult.ErrorMessage,
                        LocalizationHelper.Translate("OK"));
                    return false;
                }

                // Hantera bekräftelse för ersättning
                if (conflictResult.RequiresConfirmation)
                {
                    bool userConfirmed = await Shell.Current.DisplayAlert(
                        LocalizationHelper.Translate("ReplaceShift_Title"),
                        conflictResult.ConfirmationMessage,
                        LocalizationHelper.Translate("ReplaceShift_Confirm"),
                        LocalizationHelper.Translate("Cancel"));

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
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("Error"),
                    string.Format(LocalizationHelper.Translate("Error_CheckConflicts"), ex.Message),
                    LocalizationHelper.Translate("OK"));
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
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("Error"),
                    string.Format(LocalizationHelper.Translate("Error_DeleteShift"), ex.Message),
                    LocalizationHelper.Translate("OK"));
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

