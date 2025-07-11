using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Handlers;
using System.ComponentModel;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels.ShiftTypes
{
    public class VacationViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly VacationHandler _vacationHandler;

        // Events
        public event Action ValidationChanged;

        // Context
        private DateTime _selectedDate;
        private JobProfile _activeJob;

        // Semester-specifika fields (SUPER FÖRENKLADE)
        private VacationType _selectedVacationType = VacationType.PaidVacation;

        // Validation
        private string _validationMessage = "";
        private string _semesterKvot = "1.0";
        #endregion

        #region Constructor
        public VacationViewModel(VacationHandler vacationHandler)
        {
            _vacationHandler = vacationHandler;
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

        public string ActiveJobTitle => ActiveJob?.JobTitle ?? "Inget aktivt jobb";

        public string EmploymentTypeInfo => ActiveJob?.EmploymentType switch
        {
            EmploymentType.Permanent => "Fast anställd - Har rätt till betald semester",
            EmploymentType.Temporary => "Timanställd - Får semesterersättning vid lön (12%)",
            _ => ""
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
            }
        }

        public List<string> VacationTypeDisplayNames { get; } = new List<string>
        {
            "Betald semester",
            "Obetald ledighet"
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

        public string VacationExplanation
        {
            get
            {
                if (ActiveJob == null)
                    return "";

                return (_selectedVacationType, ActiveJob.EmploymentType) switch
                {
                    (VacationType.PaidVacation, EmploymentType.Permanent) =>
                        "💰 Betald semester - Lön beräknas i rapporten baserat på aktuell månadslön",
                    (VacationType.PaidVacation, EmploymentType.Temporary) =>
                        "⚠️ Timanställd kan inte ha betald semester - Välj 'Obetald ledighet'",
                    (VacationType.UnpaidVacation, _) =>
                        "🏖️ Obetald ledighet - Registreras utan lön",
                    _ => ""
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

        public void UpdateContext(DateTime selectedDate, JobProfile activeJob)
        {
            SelectedDate = selectedDate;
            ActiveJob = activeJob;
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
        public async Task<bool> SaveVacation()
        {
            try
            {
                // Konvertera kvot till decimal
                if (!decimal.TryParse(SemesterKvot, out decimal kvot))
                    return false;

                // Skicka kvot till handler
                return await _vacationHandler.SaveSimpleVacation(
                    SelectedDate,
                    ActiveJob,
                    _selectedVacationType, kvot);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fel i SaveVacation: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Private Methods

        private void RefreshUIProperties()
        {
            OnPropertyChanged(nameof(ShowUnpaidVacationFields));
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
                return "Inget aktivt jobb";

            if (_selectedVacationType == VacationType.PaidVacation &&
                ActiveJob.EmploymentType == EmploymentType.Temporary)
                return "Timanställd kan inte ha betald semester - välj 'Obetald ledighet'";

            // Validera semesterkvot
            if (!decimal.TryParse(SemesterKvot, out decimal kvot) || kvot <= 0)
                return "Semesterkvot måste vara ett positivt tal (t.ex. 1.0)";

            return "";
        }

        private async void OnSaveVacation()
        {
            try
            {
                var success = await SaveVacation();
                if (success)
                {
                    await Shell.Current.DisplayAlert("✅ Sparat!",
                        "Semester registrerad - lön beräknas i rapporten", "OK");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.DisplayAlert("❌ Fel",
                        "Kunde inte spara semester", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("❌ Fel",
                    $"Kunde inte spara: {ex.Message}", "OK");
            }
        }

        private string GetVacationTypeDisplayName(VacationType vacationType)
        {
            return vacationType switch
            {
                VacationType.PaidVacation => "Betald semester",
                VacationType.UnpaidVacation => "Obetald ledighet",
                _ => "Betald semester"
            };
        }

        private VacationType GetVacationTypeFromDisplay(string displayName)
        {
            return displayName switch
            {
                "Betald semester" => VacationType.PaidVacation,
                "Obetald ledighet" => VacationType.UnpaidVacation,
                _ => VacationType.PaidVacation
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
