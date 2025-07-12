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

        public string ActiveJobTitle => ActiveJob?.JobTitle ?? "Inget aktivt jobb";

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

        public List<string> SickTypeDisplayNames { get; } = new List<string>
        {
            "Skulle ha jobbat (sjuk)",              // ShouldHaveWorked
            "Jobbat delvis (delvis sjuk)",          // WorkedPartially  
            "Skulle varit ledig (sjuk på ledighet)" // WouldBeFree
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
                    SickLeaveType.ShouldHaveWorked => "🤒 Skulle jobbat men var sjuk - Karensavdrag första dagen, sedan 80% sjuklön",
                    SickLeaveType.WorkedPartially => "⚡ Jobbat delvis - Vanlig lön för arbetade timmar + sjuklön för resten",
                    SickLeaveType.WouldBeFree => "🏠 Var sjuk på ledighet - Ingen sjuklön (skulle ändå varit ledig)",
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
                        ? $"⚠️ Karensavdrag: {sickResult.KarensDeduction:C}"
                        : "✅ Ingen karensavdrag";
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
                KarensInfo = "Fel vid beräkning";
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
                return "Inget aktivt jobb";

            if (_selectedSickType == SickLeaveType.WorkedPartially)
            {
                var worked = WorkedEndTime - WorkedStartTime;
                var scheduled = ScheduledEndTime - ScheduledStartTime;

                if (worked <= TimeSpan.Zero)
                    return "Arbetade timmar måste vara större än 0";
                if (worked >= scheduled)
                    return "Arbetade timmar måste vara mindre än planerade";
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
                    await Shell.Current.DisplayAlert("✅ Sparat!", "Sjukdag registrerad", "OK");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.DisplayAlert("❌ Fel", "Kunde inte spara sjukdag", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("❌ Fel", $"Kunde inte spara: {ex.Message}", "OK");
            }
        }

        private string GetSickTypeDisplayName(SickLeaveType sickType)
        {
            return sickType switch
            {
                SickLeaveType.ShouldHaveWorked => "Skulle ha jobbat (sjuk)",           
                SickLeaveType.WorkedPartially => "Jobbat delvis (delvis sjuk)",       
                SickLeaveType.WouldBeFree => "Skulle varit ledig (sjuk på ledighet)", 
                _ => "Skulle ha jobbat (sjuk)"
            };
        }

        private SickLeaveType GetSickTypeFromDisplay(string displayName)
        {
            return displayName switch
            {
                "Skulle ha jobbat (sjuk)" => SickLeaveType.ShouldHaveWorked,           
                "Jobbat delvis (delvis sjuk)" => SickLeaveType.WorkedPartially,        
                "Skulle varit ledig (sjuk på ledighet)" => SickLeaveType.WouldBeFree,  
                _ => SickLeaveType.ShouldHaveWorked
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
