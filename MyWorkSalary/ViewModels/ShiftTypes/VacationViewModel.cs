using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Handlers;
using MyWorkSalary.Services.Interfaces;
using System.ComponentModel;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels.ShiftTypes
{
    public class VacationViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly VacationHandler _vacationHandler;
        private readonly IShiftValidationService _validationService;

        // Events
        public event Action ValidationChanged;

        // Context
        private DateTime _selectedDate;
        private JobProfile _activeJob;
        private string _remainingVacationText = "";

        // Semester-specifika fields
        private VacationType _selectedVacationType = VacationType.PaidVacation;

        // Validation
        private string _validationMessage = "";
        private string _semesterKvot = "1.0";
        private string _plannedWorkHours = "8.0";
        #endregion

        #region Constructor
        public VacationViewModel(VacationHandler vacationHandler, IShiftValidationService validationService)
        {
            _vacationHandler = vacationHandler;
            _validationService = validationService;

            SaveVacationCommand = new Command(OnSaveVacation, CanSaveVacation);
        }
        #endregion

        #region Public Properties

        // Context properties
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
                TriggerValidationChanged();
            }
        }

        public JobProfile ActiveJob
        {
            get => _activeJob;
            set
            {
                _activeJob = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActiveJobTitle));
                OnPropertyChanged(nameof(EmploymentTypeInfo));
                OnPropertyChanged(nameof(VacationExplanation));
                TriggerValidationChanged();
            }
        }

        public string ActiveJobTitle => ActiveJob?.JobTitle ?? LocalizationHelper.Translate("NoActiveJob");

        public string EmploymentTypeInfo => ActiveJob?.EmploymentType switch
        {
            EmploymentType.Permanent => LocalizationHelper.Translate("Vacation_PermanentInfo"),
            EmploymentType.Temporary => LocalizationHelper.Translate("Vacation_TemporaryInfo"),
            _ => string.Empty
        };

        // Semestertyp
        public VacationType SelectedVacationType
        {
            get => _selectedVacationType;
            set
            {
                _selectedVacationType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VacationTypeDisplayName));
                RefreshUIProperties();
                TriggerValidationChanged();

                if (ActiveJob != null)
                {
                    _ = Task.Run(async () => await LoadRemainingVacationDays());
                }
            }
        }

        public List<string> VacationTypeDisplayNames { get; } = new List<string>
        {
            LocalizationHelper.Translate("Vacation_Type_Paid"),
            LocalizationHelper.Translate("Vacation_Type_Unpaid")
        };

        public string VacationTypeDisplayName
        {
            get => GetVacationTypeDisplayName(_selectedVacationType);
            set
            {
                _selectedVacationType = GetVacationTypeFromDisplay(value);
                OnPropertyChanged();
                RefreshUIProperties();
                TriggerValidationChanged();
            }
        }

        // UI Properties 
        public bool ShowUnpaidVacationFields => _selectedVacationType == VacationType.UnpaidVacation;
        public bool ShowPaidVacationFields => _selectedVacationType == VacationType.PaidVacation;  
        public bool IsPaidVacation => _selectedVacationType == VacationType.PaidVacation;          
        public bool IsUnpaidVacation => _selectedVacationType == VacationType.UnpaidVacation;      

        public string VacationExplanation
        {
            get
            {
                if (ActiveJob == null)
                    return string.Empty;

                return (_selectedVacationType, ActiveJob.EmploymentType) switch
                {
                    (VacationType.PaidVacation, EmploymentType.Permanent) =>
                        LocalizationHelper.Translate("Vacation_Explanation_PaidPermanent"),
                    (VacationType.PaidVacation, EmploymentType.Temporary) =>
                        LocalizationHelper.Translate("Vacation_Explanation_PaidTemporary"),
                    (VacationType.UnpaidVacation, _) =>
                        LocalizationHelper.Translate("Vacation_Explanation_Unpaid"),
                    _ => string.Empty
                };
            }
        }

        public string SemesterKvot
        {
            get => _semesterKvot;
            set
            {
                _semesterKvot = value;
                OnPropertyChanged();
                TriggerValidationChanged();
            }
        }

        public string PlannedWorkHours
        {
            get => _plannedWorkHours;
            set
            {
                _plannedWorkHours = value;
                OnPropertyChanged();
                TriggerValidationChanged();
            }
        }
        public string RemainingVacationText
        {
            get => _remainingVacationText;
            set
            {
                _remainingVacationText = value;
                OnPropertyChanged();
            }
        }

        public bool ShowRemainingDays => _selectedVacationType == VacationType.PaidVacation &&
                                        ActiveJob != null;

        // Validation
        public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

        public string ValidationMessage
        {
            get => _validationMessage;
            set
            {
                _validationMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasValidationMessage));
            }
        }
        #endregion

        #region Commands
        public ICommand SaveVacationCommand { get; }
        #endregion

        #region Public Methods

        public async void UpdateContext(DateTime selectedDate, JobProfile activeJob)
        {
            SelectedDate = selectedDate;
            ActiveJob = activeJob;

            // Ladda återstående dagar för betald semester
            if (activeJob != null)
            {
                await LoadRemainingVacationDays();
            }

        }

        public bool CanSave()
        {
            if (ActiveJob == null)
                return false;

            // Timanställd kan inte ha betald semester
            if (_selectedVacationType == VacationType.PaidVacation &&
                ActiveJob.EmploymentType == EmploymentType.Temporary)
                return false;

            return true;
        }

        // SaveVacation
        public async Task<(bool Success, string Message)> SaveVacation()
        {
            try
            {
                // Normalisera kvot-strängen
                var normalizedKvot = SemesterKvot.Replace(',', '.');
                if (!decimal.TryParse(normalizedKvot,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal kvot) || kvot <= 0)
                {
                    //LogDebug($"❌ Kunde inte konvertera kvot: '{SemesterKvot}' → '{normalizedKvot}'");
                    return (false, LocalizationHelper.Translate("Vacation_Validation_RatioPositive"));
                }

                // Hantera planerade arbetstimmar för obetald
                decimal plannedHours = 0m;
                if (_selectedVacationType == VacationType.UnpaidVacation)
                {
                    var normalizedHours = PlannedWorkHours.Replace(',', '.');
                    if (!decimal.TryParse(normalizedHours,
                        System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out plannedHours) || plannedHours < 0)
                    {
                        //LogDebug($"❌ Kunde inte konvertera planerade timmar: '{PlannedWorkHours}' → '{normalizedHours}'");
                        return (false, LocalizationHelper.Translate("Vacation_Validation_HoursPositive"));
                    }
                }

                // Anropa den nya metoden som returnerar (bool, string)
                return await _vacationHandler.SaveSimpleVacation(
                    SelectedDate,
                    ActiveJob,
                    _selectedVacationType,
                    kvot,
                    plannedHours);
            }
            catch (Exception ex)
            {
                //LogDebug($"❌ Fel i SaveVacation: {ex.Message}");
                return (false, string.Format(
                    LocalizationHelper.Translate("Vacation_Save_ErrorPrefix"),
                    ex.Message
                ));
            }
        }

        public void Reset()
        {
            _selectedDate = DateTime.Today;
            _activeJob = null;

            _selectedVacationType = VacationType.PaidVacation;
            _semesterKvot = "1.0";
            _plannedWorkHours = "8.0";
            _validationMessage = "";
            _remainingVacationText = "";

            OnPropertyChanged(nameof(SelectedDate));
            OnPropertyChanged(nameof(ActiveJob));
            OnPropertyChanged(nameof(SelectedVacationType));
            OnPropertyChanged(nameof(VacationTypeDisplayName));
            OnPropertyChanged(nameof(ShowUnpaidVacationFields));
            OnPropertyChanged(nameof(ShowPaidVacationFields));
            OnPropertyChanged(nameof(IsPaidVacation));
            OnPropertyChanged(nameof(IsUnpaidVacation));
            OnPropertyChanged(nameof(VacationExplanation));
            OnPropertyChanged(nameof(SemesterKvot));
            OnPropertyChanged(nameof(PlannedWorkHours));
            OnPropertyChanged(nameof(RemainingVacationText));
            OnPropertyChanged(nameof(ValidationMessage));

            ValidationChanged?.Invoke();
        }
        #endregion

        #region Private Methods

        private void RefreshUIProperties()
        {
            OnPropertyChanged(nameof(ShowUnpaidVacationFields));
            OnPropertyChanged(nameof(ShowPaidVacationFields));    
            OnPropertyChanged(nameof(IsPaidVacation));            
            OnPropertyChanged(nameof(IsUnpaidVacation));
            OnPropertyChanged(nameof(ShowRemainingDays));
            OnPropertyChanged(nameof(VacationExplanation));
        }

        private void TriggerValidationChanged()
        {
            ValidationChanged?.Invoke();
            ((Command)SaveVacationCommand).ChangeCanExecute();
        }

        private bool CanSaveVacation()
        {
            var canSave = CanSave();
            ValidationMessage = canSave ? "" : GetValidationError();
            return canSave;
        }

        private string GetValidationError()
        {
            if (ActiveJob == null)
                return LocalizationHelper.Translate("NoActiveJob");

            if (_selectedVacationType == VacationType.PaidVacation &&
                ActiveJob.EmploymentType == EmploymentType.Temporary)
                return LocalizationHelper.Translate("Vacation_Validation_PaidNotAllowedForHourly");

            // Validera semesterkvot (bara för betald)
            if (_selectedVacationType == VacationType.PaidVacation)
            {
                if (!decimal.TryParse(SemesterKvot, out decimal kvot) || kvot <= 0)
                    return LocalizationHelper.Translate("Vacation_Error_InvalidKvot");
            }

            // Validera planerade arbetstimmar (bara för obetald)
            if (_selectedVacationType == VacationType.UnpaidVacation)
            {
                if (!decimal.TryParse(PlannedWorkHours, out decimal hours) || hours < 0)
                    return LocalizationHelper.Translate("Vacation_Error_InvalidHours");
            }

            // Validera datum mot befintliga pass
            var (canAdd, errorMessage, conflictingShifts) = _validationService.ValidateVacationDate(
                ActiveJob.Id,
                SelectedDate,
                _selectedVacationType);

            if (!canAdd)
                return errorMessage;

            return "";
        }

        private async void OnSaveVacation()
        {
            try
            {
                // Kör validering INNAN sparning
                var validationError = GetValidationError();

                if (!string.IsNullOrEmpty(validationError))
                {
                    await Shell.Current.DisplayAlert(
                        LocalizationHelper.Translate("Vacation_Save_ErrorTitle"),
                        validationError,
                        LocalizationHelper.Translate("Ok"));
                    return;
                }

                // Använd den nya return-typen (bool, string)
                var (success, message) = await SaveVacation();

                if (success)
                {
                    await Shell.Current.DisplayAlert(
                        LocalizationHelper.Translate("Vacation_Save_SuccessTitle"),
                        message,
                        LocalizationHelper.Translate("Ok"));
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    // Visa det riktiga felmeddelandet från VacationHandler
                    await Shell.Current.DisplayAlert(
                        LocalizationHelper.Translate("Vacation_Save_ErrorTitle"),
                        message,
                        LocalizationHelper.Translate("Ok"));
                }
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Exception i OnSaveVacation: {ex.Message}");
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("Vacation_Save_UnexpectedError"),
                    LocalizationHelper.Translate("Vacation_Save_UnexpectedErrorMessage"),
                    LocalizationHelper.Translate("Ok"));
            }
        }

        private string GetVacationTypeDisplayName(VacationType vacationType)
        {
            return vacationType switch
            {
                VacationType.PaidVacation => LocalizationHelper.Translate("Vacation_Type_Paid"),
                VacationType.UnpaidVacation => LocalizationHelper.Translate("Vacation_Type_Unpaid"),
                _ => LocalizationHelper.Translate("Vacation_Type_Paid")
            };
        }

        private VacationType GetVacationTypeFromDisplay(string displayName)
        {
            var paid = LocalizationHelper.Translate("Vacation_Type_Paid");
            var unpaid = LocalizationHelper.Translate("Vacation_Type_Unpaid");

            if (displayName == paid)
                return VacationType.PaidVacation;
            if (displayName == unpaid)
                return VacationType.UnpaidVacation;

            return VacationType.PaidVacation;
        }

        private async Task LoadRemainingVacationDays()
        {
            try
            {
                if (_selectedVacationType != VacationType.PaidVacation || ActiveJob == null)
                {
                    RemainingVacationText = "";
                    return;
                }

                var remaining = await _vacationHandler.GetRemainingVacationDays(ActiveJob.Id);
                var total = ActiveJob.VacationDaysPerYear + (ActiveJob.InitialVacationBalance ?? 0);

                RemainingVacationText = string.Format(
                    LocalizationHelper.Translate("Vacation_RemainingDays"),
                    remaining,
                    total);
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Fel vid laddning av återstående dagar: {ex.Message}");
                RemainingVacationText = LocalizationHelper.Translate("Vacation_RemainingDaysError");
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

        #region Debug Helper
        /// <summary>
        /// Debug-logging som fungerar på både emulator och riktig enhet
        /// Aktivera/inaktivera genom att ändra DEBUG_VACATION konstanten
        /// </summary>
        private const bool DEBUG_VACATION = false; // Sätt till true för debugging

        private void LogDebug(string message)
        {
            if (!DEBUG_VACATION)
                return;

#if ANDROID
            Android.Util.Log.Debug("VacationViewModel", message);
#else
    System.Diagnostics.Debug.WriteLine(message);
#endif
        }
        #endregion
    }
}
