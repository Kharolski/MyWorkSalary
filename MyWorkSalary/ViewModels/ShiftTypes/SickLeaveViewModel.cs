using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Handlers;
using System.ComponentModel;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels.ShiftTypes
{
    public class SickLeaveViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly SickLeaveHandler _sickLeaveHandler;

        // Event för att meddela när validering ändras
        public event Action ValidationChanged;

        // Context från parent
        private DateTime _selectedDate;
        private JobProfile _activeJob;

        // Sjuk-specifika fields
        private SickLeaveType _selectedSickType = SickLeaveType.ShouldHaveWorked;
        private TimeSpan _workedStartTime = new TimeSpan(8, 0, 0);
        private TimeSpan _workedEndTime = new TimeSpan(12, 0, 0);
        private TimeSpan _scheduledStartTime = new TimeSpan(8, 0, 0);
        private TimeSpan _scheduledEndTime = new TimeSpan(16, 0, 0);
        private string _notes = string.Empty;

        // Beräknade värden
        private decimal _calculatedPay = 0;
        private string _karensInfo = string.Empty;

        // Validation
        private string _validationMessage = "";
        #endregion

        #region Constructor
        public SickLeaveViewModel(SickLeaveHandler sickLeaveHandler)
        {
            _sickLeaveHandler = sickLeaveHandler;

            // Commands
            SaveSickLeaveCommand = new Command(OnSaveSickLeave, CanSaveSickLeave);

            // Initial calculation
            CalculateSickPay();

            LocalizationHelper.LanguageChanged += () =>
            {
                OnPropertyChanged(nameof(SickTypeDisplayNames));
                OnPropertyChanged(nameof(SickTypeDisplayName));
                RefreshUIProperties();
            };
        }
        #endregion

        #region Public Properties

        // Context properties (uppdateras från parent)
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
                CalculateSickPay();
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
                CalculateSickPay();
            }
        }

        public string ActiveJobTitle => ActiveJob?.JobTitle ?? LocalizationHelper.Translate("NoActiveJob");

        // Sjuktyp
        private void RefreshUIProperties()
        {
            OnPropertyChanged(nameof(ShowPartialTimeFields));
            OnPropertyChanged(nameof(ShowWorkedTimeFields));
            OnPropertyChanged(nameof(ShowScheduledTimeFields));
            OnPropertyChanged(nameof(SickLeaveExplanation));
        }

        public SickLeaveType SelectedSickType
        {
            get => _selectedSickType;
            set
            {
                _selectedSickType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SickTypeDisplayName));
                RefreshUIProperties();
                CalculateSickPay();
            }
        }

        public List<string> SickTypeDisplayNames =>
            new()
            {
                LocalizationHelper.Translate("SickLeave_ShouldHaveWorked"),   // ShouldHaveWorked
                LocalizationHelper.Translate("SickLeave_WorkedPartially"),    // WorkedPartially
                LocalizationHelper.Translate("SickLeave_WouldBeFree")         // WouldBeFree
            };

        public string SickTypeDisplayName
        {
            get => GetSickTypeDisplayName(_selectedSickType);
            set
            {
                _selectedSickType = GetSickTypeFromDisplay(value);
                OnPropertyChanged();
                RefreshUIProperties();
                CalculateSickPay();
            }
        }

        // Arbetstider för delvis sjuk
        public TimeSpan WorkedStartTime
        {
            get => _workedStartTime;
            set
            {
                _workedStartTime = value;
                OnPropertyChanged();
                CalculateSickPay();
            }
        }

        public TimeSpan WorkedEndTime
        {
            get => _workedEndTime;
            set
            {
                _workedEndTime = value;
                OnPropertyChanged();
                CalculateSickPay();
            }
        }

        public TimeSpan ScheduledStartTime
        {
            get => _scheduledStartTime;
            set
            {
                _scheduledStartTime = value;
                OnPropertyChanged();
                CalculateSickPay();
            }
        }

        public TimeSpan ScheduledEndTime
        {
            get => _scheduledEndTime;
            set
            {
                _scheduledEndTime = value;
                OnPropertyChanged();
                CalculateSickPay();
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

        // UI Properties
        public bool ShowWorkedTimeFields => _selectedSickType == SickLeaveType.WorkedPartially;
        
        public bool ShowScheduledTimeFields =>
            _selectedSickType == SickLeaveType.WorkedPartially ||
            _selectedSickType == SickLeaveType.ShouldHaveWorked;

        public bool ShowPartialTimeFields => ShowWorkedTimeFields || ShowScheduledTimeFields;

        public string SickLeaveExplanation
        {
            get
            {
                return _selectedSickType switch
                {
                    SickLeaveType.ShouldHaveWorked => LocalizationHelper.Translate("SickLeave_Explanation_ShouldHaveWorked"),
                    SickLeaveType.WorkedPartially => LocalizationHelper.Translate("SickLeave_Explanation_WorkedPartially"),
                    SickLeaveType.WouldBeFree => LocalizationHelper.Translate("SickLeave_Explanation_WouldBeFree"),
                    _ => ""
                };
            }
        }

        // Beräknade värden
        public decimal CalculatedPay
        {
            get => _calculatedPay;
            set
            {
                _calculatedPay = value;
                OnPropertyChanged();
            }
        }

        public string KarensInfo
        {
            get => _karensInfo;
            set
            {
                _karensInfo = value;
                OnPropertyChanged();
            }
        }

        // Validation feedback
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
        public ICommand SaveSickLeaveCommand { get; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Uppdaterar context från parent ViewModel
        /// </summary>
        public void UpdateContext(DateTime selectedDate, JobProfile activeJob)
        {
            SelectedDate = selectedDate;
            ActiveJob = activeJob;
        }

        /// <summary>
        /// Validerar om sjukdag kan sparas
        /// </summary>
        public bool CanSave()
        {
            if (ActiveJob == null)
                return false;

            // Delvis sjuk kräver arbetstider
            if (_selectedSickType == SickLeaveType.WorkedPartially)
            {
                var workedHours = WorkedEndTime - WorkedStartTime;
                var scheduledHours = ScheduledEndTime - ScheduledStartTime;

                return workedHours > TimeSpan.Zero &&
                       scheduledHours > TimeSpan.Zero &&
                       workedHours < scheduledHours;
            }

            return true;
        }

        /// <summary>
        /// Public SaveSickLeave för AddShiftViewModel
        /// </summary>
        public async Task<bool> SaveSickLeave()
        {
            try
            {
                TimeSpan? workedStartTime = null;
                TimeSpan? workedEndTime = null;
                TimeSpan? scheduledStartTime = null;
                TimeSpan? scheduledEndTime = null;

                if (_selectedSickType == SickLeaveType.WorkedPartially)
                {
                    workedStartTime = WorkedStartTime;
                    workedEndTime = WorkedEndTime;
                    scheduledStartTime = ScheduledStartTime;
                    scheduledEndTime = ScheduledEndTime;

                    // Hantera över midnatt
                    if (workedEndTime < workedStartTime)
                        workedEndTime = workedEndTime.Value.Add(TimeSpan.FromDays(1));
                    if (scheduledEndTime < scheduledStartTime)
                        scheduledEndTime = scheduledEndTime.Value.Add(TimeSpan.FromDays(1));
                }
                else if (_selectedSickType == SickLeaveType.ShouldHaveWorked)
                {
                    scheduledStartTime = ScheduledStartTime;
                    scheduledEndTime = ScheduledEndTime;

                    // Hantera över midnatt
                    if (scheduledEndTime < scheduledStartTime)
                        scheduledEndTime = scheduledEndTime.Value.Add(TimeSpan.FromDays(1));
                }

                // HANTERA TUPLE-RETURVÄRDET
                var (workShift, sickLeave) = await _sickLeaveHandler.HandleSickLeave(
                    SelectedDate,
                    ActiveJob,
                    _selectedSickType,
                    workedStartTime,
                    workedEndTime,
                    scheduledStartTime,
                    scheduledEndTime);

                // KONTROLLERA BÅDA OBJEKTEN
                return workShift != null && sickLeave != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fel i SaveSickLeave: {ex.Message}");
                return false;
            }
        }

        public void Reset()
        {
            // Återställ enum
            _selectedSickType = SickLeaveType.ShouldHaveWorked;
            OnPropertyChanged(nameof(SelectedSickType));
            OnPropertyChanged(nameof(SickTypeDisplayName));

            // Återställ arbetstider
            WorkedStartTime = new TimeSpan(8, 0, 0);
            WorkedEndTime = new TimeSpan(12, 0, 0);
            ScheduledStartTime = new TimeSpan(8, 0, 0);
            ScheduledEndTime = new TimeSpan(16, 0, 0);

            // Övriga fält
            Notes = string.Empty;
            CalculatedPay = 0;
            KarensInfo = "";
            ValidationMessage = "";

            // UI refresh (visar rätt fält för korrekt sjuktyp)
            RefreshUIProperties();

            // Räkna om
            CalculateSickPay();
        }
        #endregion

        #region Private Methods

        private async void CalculateSickPay()
        {
            if (ActiveJob == null)
            {
                CalculatedPay = 0;
                KarensInfo = "";
                return;
            }

            try
            {
                TimeSpan? workedHours = null;
                TimeSpan? scheduledHours = null;

                // Sätt timmar baserat på sjuktyp
                if (_selectedSickType == SickLeaveType.WorkedPartially)
                {
                    // Använd faktiska tider från UI
                    workedHours = WorkedEndTime - WorkedStartTime;
                    scheduledHours = ScheduledEndTime - ScheduledStartTime;

                    // Fixa negativa tider (över midnatt)
                    if (workedHours < TimeSpan.Zero)
                        workedHours = workedHours.Value.Add(TimeSpan.FromDays(1));
                    if (scheduledHours < TimeSpan.Zero)
                        scheduledHours = scheduledHours.Value.Add(TimeSpan.FromDays(1));
                }
                else if (_selectedSickType == SickLeaveType.ShouldHaveWorked)
                {
                    workedHours = TimeSpan.Zero;
                    scheduledHours = ScheduledEndTime - ScheduledStartTime;

                    if (scheduledHours < TimeSpan.Zero)
                        scheduledHours = scheduledHours.Value.Add(TimeSpan.FromDays(1));
                }
                else if (_selectedSickType == SickLeaveType.WouldBeFree)
                {
                    // Skulle varit ledig = ingen betalning
                    scheduledHours = TimeSpan.Zero;
                    workedHours = TimeSpan.Zero;
                }

                // Använd samma enum - ingen konvertering behövs!
                var sickResult = await _sickLeaveHandler.CalculateSickPayForUI(
                    ActiveJob, _selectedSickType, workedHours, scheduledHours);

                if (string.IsNullOrEmpty(sickResult.ErrorMessage))
                {
                    CalculatedPay = sickResult.TotalPay;
                    KarensInfo = sickResult.HasKarensDeduction
                        ? string.Format(LocalizationHelper.Translate("SickLeave_KarensDeduction"), sickResult.KarensDeduction)
                        : LocalizationHelper.Translate("SickLeave_NoKarens");
                }
                else
                {
                    CalculatedPay = 0;
                    KarensInfo = sickResult.ErrorMessage;
                }

                // Uppdatera command
                ((Command)SaveSickLeaveCommand).ChangeCanExecute();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fel i CalculateSickPay: {ex.Message}");
                CalculatedPay = 0;
                KarensInfo = LocalizationHelper.Translate("SickLeave_CalcError");
            }
        }

        private bool CanSaveSickLeave()
        {
            var canSave = CanSave();

            ValidationMessage = canSave ? "" : GetValidationError();
            OnPropertyChanged(nameof(ValidationMessage));

            return canSave;
        }

        private string GetValidationError()
        {
            if (ActiveJob == null)
                return LocalizationHelper.Translate("NoActiveJob");

            if (_selectedSickType == SickLeaveType.WorkedPartially)
            {
                var worked = WorkedEndTime - WorkedStartTime;
                var scheduled = ScheduledEndTime - ScheduledStartTime;

                if (worked <= TimeSpan.Zero)
                    return LocalizationHelper.Translate("SickLeave_Validation_WorkedTooSmall");
                if (worked >= scheduled)
                    return LocalizationHelper.Translate("SickLeave_Validation_WorkedTooLarge");
            }

            return "";
        }

        private async void OnSaveSickLeave()
        {
            try
            {
                // Använd den nya SaveSickLeave() metoden
                var success = await SaveSickLeave();

                if (success)
                {
                    await Shell.Current.DisplayAlert(
                        LocalizationHelper.Translate("SaveSuccess"),
                        LocalizationHelper.Translate("SickLeave_Save_Message"),
                        LocalizationHelper.Translate("OK")
                    );
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.DisplayAlert(
                        LocalizationHelper.Translate("Error"),
                        LocalizationHelper.Translate("SickLeave_Save_ErrorMessage"),
                        LocalizationHelper.Translate("OK")
                    );
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("Error"),
                    string.Format(LocalizationHelper.Translate("SickLeave_Save_Exception"), ex.Message),
                    LocalizationHelper.Translate("OK")
                );
            }
        }

        private string GetSickTypeDisplayName(SickLeaveType sickType)
        {
            return sickType switch
            {
                SickLeaveType.ShouldHaveWorked => LocalizationHelper.Translate("SickLeave_ShouldHaveWorked"),
                SickLeaveType.WorkedPartially => LocalizationHelper.Translate("SickLeave_WorkedPartially"),
                SickLeaveType.WouldBeFree => LocalizationHelper.Translate("SickLeave_WouldBeFree"),
                _ => LocalizationHelper.Translate("SickLeave_ShouldHaveWorked")
            };
        }

        private SickLeaveType GetSickTypeFromDisplay(string displayName)
        {
            if (displayName == LocalizationHelper.Translate("SickLeave_ShouldHaveWorked"))
                return SickLeaveType.ShouldHaveWorked;

            if (displayName == LocalizationHelper.Translate("SickLeave_WorkedPartially"))
                return SickLeaveType.WorkedPartially;

            if (displayName == LocalizationHelper.Translate("SickLeave_WouldBeFree"))
                return SickLeaveType.WouldBeFree;

            return SickLeaveType.ShouldHaveWorked;
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
