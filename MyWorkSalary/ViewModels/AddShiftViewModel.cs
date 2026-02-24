using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Handlers;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Services.Premium;
using MyWorkSalary.ViewModels.ShiftTypes;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public class AddShiftViewModel : BaseViewModel
    {
        #region Private Fields
        private readonly IJobProfileRepository _jobProfileRepository;
        private readonly IWorkShiftRepository _workShiftRepository;

        private readonly IPremiumService _premiumService;

        private readonly SickLeaveHandler _sickLeaveHandler;

        // Injicerade ViewModels
        public SickLeaveViewModel SickLeaveVM { get; }
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
            IOBEventRepository obEventRepository,
            IOBRateRepository obRateRepository,
            IOBEventService obEventService,
            IPremiumService premiumService,
            ShiftTypeHandler shiftTypeHandler,
            SickLeaveHandler sickLeaveHandler,
            SickLeaveViewModel sickLeaveViewModel,
            OnCallViewModel onCallViewModel, 
            VacationViewModel vacationViewModel,
            HolidayService holidayService)
        {
            _jobProfileRepository = jobProfileRepository;
            _workShiftRepository = workShiftRepository;
            _premiumService = premiumService;
            _sickLeaveHandler = sickLeaveHandler;
            SickLeaveVM = sickLeaveViewModel;
            OnCallVM = onCallViewModel;
            VacationVM = vacationViewModel;

            RegularShiftVM = new RegularShiftViewModel(
                workShiftRepository, 
                calculationService, 
                obEventRepository, 
                obRateRepository, 
                obEventService,
                premiumService,
                holidayService);

            // Lyssna på CanSave från RegularShiftVM
            RegularShiftVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(RegularShiftVM.CanSave))
                    ValidateAndUpdateCanSave();

                if (e.PropertyName == nameof(RegularShiftVM.ShowCalculation))
                    OnPropertyChanged(nameof(ShowGeneralCalculation));

                if (e.PropertyName == nameof(RegularShiftVM.CalculationSummary))
                    OnPropertyChanged(nameof(CalculationSummary));
            };

            // Prenumerera på ValidationChanged events
            SickLeaveVM.ValidationChanged += () => ValidateAndUpdateCanSave();
            OnCallVM.ValidationChanged += () => ValidateAndUpdateCanSave();
            VacationVM.ValidationChanged += () => ValidateAndUpdateCanSave();

            // Commands
            SaveCommand = new Command(OnSave, CanExecuteSave);
            CancelCommand = new Command(OnCancel);

            LoadActiveJob();

            // Språkändring event
            LocalizationHelper.LanguageChanged += () => OnLanguageChanged();
            OnLanguageChanged();
        }
        #endregion

        #region Properties
        #region Visibility Properties
        // Sjukskrivning
        public bool ShowSickLeaveForm => SelectedShiftType == ShiftType.SickLeave;

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
        public bool CanSaveNow => CanExecuteSave();
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
        public ObservableCollection<string> ShiftTypeDisplayNames { get; } = new ObservableCollection<string>();

        public string SelectedShiftTypeDisplay
        {
            get => _selectedShiftTypeDisplay;
            set
            {
                _selectedShiftTypeDisplay = value;
                _selectedShiftType = GetShiftTypeFromDisplay(value);
                OnPropertyChanged();

                // RESET alla child viewmodels när man byter tab
                SickLeaveVM.Reset();
                OnCallVM.Reset();
                VacationVM.Reset();
                RegularShiftVM.Reset();

                // Uppdatera UI och context
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
        public void LoadActiveJob()
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
                ShiftType.OnCall => OnCallVM.CanSave(),
                ShiftType.Vacation => VacationVM.CanSave(),
                ShiftType.Regular => RegularShiftVM.CanSave,
                _ => false
            };

            _canSave = canSaveResult;
            ((Command)SaveCommand).ChangeCanExecute();
            OnPropertyChanged(nameof(CanSaveNow));
        }

        private bool CanExecuteSave()
        {
            return SelectedShiftType switch
            {
                ShiftType.Regular => RegularShiftVM.CanSave,
                ShiftType.SickLeave => SickLeaveVM.CanSave(),
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
        private async void OnLanguageChanged()
        {
            try
            {
                ShiftTypeDisplayNames.Clear();
                ShiftTypeDisplayNames.Add(LocalizationHelper.Translate("ShiftType_Add_Regular"));
                
                if (_premiumService.IsPremium || _premiumService.IsSubscriber)
                {
                    ShiftTypeDisplayNames.Add(LocalizationHelper.Translate("ShiftType_OnCall"));
                    //ShiftTypeDisplayNames.Add(LocalizationHelper.Translate("ShiftType_Vacation"));
                }
                if(_premiumService.IsSubscriber)
                {
                    //ShiftTypeDisplayNames.Add(LocalizationHelper.Translate("ShiftType_SickLeave"));
                }

                // Uppdatera endast texten som visas, utan att trigga Reset-logiken i settern
                _selectedShiftTypeDisplay = _selectedShiftType switch
                {
                    ShiftType.Regular => LocalizationHelper.Translate("ShiftType_Add_Regular"),
                    ShiftType.SickLeave => LocalizationHelper.Translate("ShiftType_SickLeave"),
                    ShiftType.Vacation => LocalizationHelper.Translate("ShiftType_Vacation"),
                    ShiftType.OnCall => LocalizationHelper.Translate("ShiftType_OnCall"),
                    _ => LocalizationHelper.Translate("ShiftType_Add_Regular")
                };

                OnPropertyChanged(nameof(SelectedShiftTypeDisplay));

                // Se till att Save-knappen uppdateras efter språkbyte
                ValidateAndUpdateCanSave();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OnLanguageChanged] Error: {ex.Message}");
            }
        }

        private ShiftType GetShiftTypeFromDisplay(string displayName)
        {
            return displayName switch
            {
                _ when displayName == LocalizationHelper.Translate("ShiftType_Add_Regular") => ShiftType.Regular,
                _ when displayName == LocalizationHelper.Translate("ShiftType_OnCall")
                    && (_premiumService.IsPremium || _premiumService.IsSubscriber) => ShiftType.OnCall,
                _ when displayName == LocalizationHelper.Translate("ShiftType_SickLeave")
                    && (_premiumService.IsSubscriber) => ShiftType.SickLeave,
                _ when displayName == LocalizationHelper.Translate("ShiftType_Vacation")
                    && (_premiumService.IsPremium || _premiumService.IsSubscriber) => ShiftType.Vacation,
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
            OnPropertyChanged(nameof(ShowOnCallForm));
            OnPropertyChanged(nameof(ShowVacationForm));
            OnPropertyChanged(nameof(CalculationSummary));

            // SickLeave och Vacation är AVSTÄNGDA tills vidare
            if (SelectedShiftType == ShiftType.SickLeave || SelectedShiftType == ShiftType.Vacation)
            {
                SelectedShiftType = ShiftType.Regular;
                _selectedShiftTypeDisplay = LocalizationHelper.Translate("ShiftType_Add_Regular");
                OnPropertyChanged(nameof(SelectedShiftTypeDisplay));
            }

            // OnCall är premium, så tvinga tillbaka
            if (SelectedShiftType == ShiftType.OnCall && !(_premiumService.IsPremium || _premiumService.IsSubscriber))
            {
                // Tvinga tillbaka till Regular
                SelectedShiftType = ShiftType.Regular;
                _selectedShiftTypeDisplay = LocalizationHelper.Translate("ShiftType_Add_Regular");
                OnPropertyChanged(nameof(SelectedShiftTypeDisplay));
                return;
            }


            // Uppdatera context för child ViewModels
            if (SelectedShiftType == ShiftType.SickLeave)
            {
                SickLeaveVM.UpdateContext(SelectedDate, ActiveJob);
            }
            else if (SelectedShiftType == ShiftType.OnCall) 
            {
                OnCallVM.UpdateContext(SelectedDate, ActiveJob);
            }
            else if (SelectedShiftType == ShiftType.Vacation)   
            {
                VacationVM.UpdateContext(SelectedDate, ActiveJob);
            }

            // Uppdatera Save-knappens CanExecute direkt
            ValidateAndUpdateCanSave();
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

        private bool _hasInitialized;
        public void OnPageAppearing()
        {
            RefreshActiveJobFromDb();

            // resetta bara första gången sidan öppnas (inte när du kommer tillbaka från Settings)
            if (!_hasInitialized)
            {
                _hasInitialized = true;

                SickLeaveVM.Reset();
                OnCallVM.Reset();
                VacationVM.Reset();
                RegularShiftVM.Reset();

                SelectedShiftType = ShiftType.Regular;
                _selectedShiftTypeDisplay = LocalizationHelper.Translate("ShiftType_Add_Regular");
                OnPropertyChanged(nameof(SelectedShiftTypeDisplay));
            }

            OnSelectedShiftTypeChanged();
            ValidateAndUpdateCanSave();
        }
        public void RefreshActiveJobFromDb()
        {
            var fresh = _jobProfileRepository.GetActiveJob();
            if (fresh == null)
                return;

            // Tvinga uppdatering även om ID är samma (för att settings kan ha ändrats)
            ActiveJob = fresh;

            // Uppdatera context för child VMs
            // Men vi kör extra säkerhet:
            SickLeaveVM.UpdateContext(SelectedDate, ActiveJob);
            OnCallVM.UpdateContext(SelectedDate, ActiveJob);
            VacationVM.UpdateContext(SelectedDate, ActiveJob);
            RegularShiftVM.ActiveJob = ActiveJob;

            ValidateAndUpdateCanSave();
        }
        #endregion
    }
}

