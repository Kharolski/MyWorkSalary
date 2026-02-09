using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Handlers;
using MyWorkSalary.Services.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels.ShiftTypes
{
    public class OnCallViewModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action ValidationChanged;

        #region Private Fields
        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly IShiftValidationService _validationService;
        private readonly OnCallHandler _onCallHandler;
        private ObservableCollection<CalloutRow> _callouts;

        private bool _disposed = false;
        private DateTime _selectedDate;
        private JobProfile _activeJob;
        private TimeSpan _standbyStartTime = new TimeSpan(18, 0, 0); // 18:00
        private TimeSpan _standbyEndTime = new TimeSpan(8, 0, 0);    // 08:00
        private string _notes = "";
        private string _validationMessage = "";
        #endregion

        #region Constructor

        public OnCallViewModel(
            IWorkShiftRepository workShiftRepository,
            IShiftValidationService validationService,
            OnCallHandler onCallHandler)
        {
            _workShiftRepository = workShiftRepository;
            _validationService = validationService;
            _onCallHandler = onCallHandler;

            LocalizationHelper.LanguageChanged += OnLanguageChanged;

        }

        #endregion

        #region Properties
        public ObservableCollection<CalloutRow> Callouts
        {
            get => _callouts ??= new ObservableCollection<CalloutRow>();
            private set
            {
                _callouts = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasCallouts));
                OnPropertyChanged(nameof(HasNoCallouts));
                OnPropertyChanged(nameof(OnCallTotalsText));
            }
        }
        private TimeSpan _newCalloutStart = new TimeSpan(21, 0, 0);
        public TimeSpan NewCalloutStart
        {
            get => _newCalloutStart;
            set { _newCalloutStart = value; OnPropertyChanged(); }
        }

        private TimeSpan _newCalloutEnd = new TimeSpan(22, 0, 0);
        public TimeSpan NewCalloutEnd
        {
            get => _newCalloutEnd;
            set { _newCalloutEnd = value; OnPropertyChanged(); }
        }

        private string? _newCalloutNotes;
        public string? NewCalloutNotes
        {
            get => _newCalloutNotes;
            set { _newCalloutNotes = value; OnPropertyChanged(); }
        }

        public TimeSpan StandbyStartTime
        {
            get => _standbyStartTime;
            set
            {
                _standbyStartTime = value;
                OnPropertyChanged();

                NotifyLocalizedProperties();
                OnPropertyChanged(nameof(OnCallTotalsText));
                ValidateInput();
                ValidationChanged?.Invoke();
            }
        }
        public TimeSpan StandbyEndTime
        {
            get => _standbyEndTime;
            set
            {
                _standbyEndTime = value;
                OnPropertyChanged();

                NotifyLocalizedProperties();
                OnPropertyChanged(nameof(OnCallTotalsText));
                ValidateInput();
                ValidationChanged?.Invoke();
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

        // Visibility
        public bool ShowActiveHoursInfo => GetTotalActiveHoursFromCallouts() > 0m;
        public bool HasCallouts => Callouts?.Count > 0;
        public bool HasNoCallouts => !HasCallouts;

        // Display texts
        public string OnCallExplanationText
        {
            get
            {
                if (_activeJob?.EmploymentType == EmploymentType.Temporary)
                {
                    return LocalizationHelper.Translate("OnCall_Explanation_Temporary");
                }
                else
                {
                    return LocalizationHelper.Translate("OnCall_Explanation_Permanent");
                }
            }
        }
        public string OnCallTotalsText
        {
            get
            {
                var standby = CalculateStandbyHours();
                var active = Callouts?.Sum(GetCalloutHours) ?? 0m;

                var h = LocalizationHelper.Translate("HoursAbbreviation");
                return $"Standby: {standby:0.##}{h} • Aktiv: {active:0.##}{h}";
            }
        }

        #endregion

        #region Commands
        public ICommand AddCalloutCommand => new Command(() =>
        {
            // Basic validation för input-raden
            if (NewCalloutEnd == NewCalloutStart)
            {
                ValidationMessage = LocalizationHelper.Translate("OnCall_Validation_CalloutZero");
                return;
            }

            var row = new CalloutRow
            {
                Start = NewCalloutStart,
                End = NewCalloutEnd,
                Notes = string.IsNullOrWhiteSpace(NewCalloutNotes) ? null : NewCalloutNotes.Trim()
            };

            Callouts.Add(row);

            // reset notes (tider kan du välja att behålla)
            NewCalloutNotes = null;

            OnPropertyChanged(nameof(HasCallouts));
            OnPropertyChanged(nameof(HasNoCallouts));
            OnPropertyChanged(nameof(OnCallTotalsText));
            OnPropertyChanged(nameof(ShowActiveHoursInfo));

            ValidateInput();
        });

        public ICommand DeleteCalloutCommand => new Command<CalloutRow>(row =>
        {
            if (row == null)
                return;

            Callouts.Remove(row);

            OnPropertyChanged(nameof(HasCallouts));
            OnPropertyChanged(nameof(HasNoCallouts));
            OnPropertyChanged(nameof(OnCallTotalsText));
            OnPropertyChanged(nameof(ShowActiveHoursInfo));

            ValidateInput();
        });
        #endregion

        #region Public Methods

        public void UpdateContext(DateTime selectedDate, JobProfile activeJob)
        {
            _selectedDate = selectedDate;
            _activeJob = activeJob;

            // Uppdatera språk- och valuta-beroende properties
            NotifyLocalizedProperties();

            OnPropertyChanged(nameof(OnCallExplanationText));
            OnPropertyChanged(nameof(OnCallTotalsText));
            OnPropertyChanged(nameof(HasCallouts));
            OnPropertyChanged(nameof(HasNoCallouts));

            // Uppdatera övriga properties
            ValidateInput();
        }

        public bool CanSave()
        {
            if (_activeJob == null)
                return false;
            if (!_activeJob.OnCallEnabled)
                return false;
            if (CalculateStandbyHours() <= 0)
                return false;
            return string.IsNullOrEmpty(ValidationMessage);
        }

        public async Task<bool> SaveOnCall()
        {
            try
            {
                if (!CanSave())
                {
                    ValidationMessage = LocalizationHelper.Translate("OnCall_Save_ErrorMissing");
                    return false;
                }

                // Använd OnCallHandler för att skapa jourpass
                var workShift = _onCallHandler.CreateOnCallShift(
                    _activeJob.Id,
                    _selectedDate,
                    _standbyStartTime,
                    _standbyEndTime,
                    Callouts,
                    _notes
                );

                return workShift != null;
            }
            catch (Exception ex)
            {
                ValidationMessage = string.Format(
                    LocalizationHelper.Translate("OnCall_Save_Exception"),
                    ex.Message
                );
                return false;
            }
        }

        public void Reset()
        {
            // Återställ core-data
            _standbyStartTime = new TimeSpan(18, 0, 0);  // 18:00 standard
            _standbyEndTime = new TimeSpan(8, 0, 0);     // 08:00 standard
            _notes = "";
            _validationMessage = "";

            // Trigger UI updates
            OnPropertyChanged(nameof(StandbyStartTime));
            OnPropertyChanged(nameof(StandbyEndTime));
            OnPropertyChanged(nameof(Notes));
            OnPropertyChanged(nameof(ValidationMessage));
            OnPropertyChanged(nameof(HasValidationMessage));

            // Lokala språkberoende properties
            NotifyLocalizedProperties();

            // Kör validering
            ValidateInput();
            ValidationChanged?.Invoke();
        }
        #endregion

        #region Private Methods
        private void OnLanguageChanged()
        {
            // Försöksskydd mot att metoden körs efter dispose
            if (_disposed)
                return;

            // Update all localized properties
            NotifyLocalizedProperties();

            // Update other properties that might depend on language
            OnPropertyChanged(nameof(HasCallouts));
        }
        private decimal GetTotalActiveHoursFromCallouts()
        {
            if (Callouts == null || Callouts.Count == 0)
                return 0m;

            decimal total = 0m;

            foreach (var c in Callouts)
            {
                var start = c.Start;
                var end = c.End;

                // Hantera om inkallning går över midnatt
                if (end <= start)
                    end = end.Add(TimeSpan.FromDays(1));

                var duration = end - start;
                var hours = (decimal)duration.TotalHours;

                if (hours > 0)
                    total += hours;
            }

            return total;
        }
        private decimal CalculateStandbyHours()
        {
            var start = _standbyStartTime;
            var end = _standbyEndTime;

            // Hantera över midnatt
            if (end <= start)
            {
                end = end.Add(TimeSpan.FromDays(1));
            }

            var duration = end - start;
            return (decimal)duration.TotalHours;
        }

        private void ValidateInput()
        {
            ValidationMessage = "";

            if (_activeJob == null)
            {
                ValidationMessage = LocalizationHelper.Translate("OnCall_Save_ErrorMissing");
                return;
            }

            // Måste vara aktiverat i settings
            if (!_activeJob.OnCallEnabled)
            {
                ValidationMessage = LocalizationHelper.Translate("OnCall_Validation_NotEnabled");
                return;
            }

            // Standby timmar
            var standbyHours = CalculateStandbyHours();
            if (standbyHours <= 0)
            {
                ValidationMessage = LocalizationHelper.Translate("OnCall_Validation_ZeroHours");
                return;
            }
            if (standbyHours > 24)
            {
                ValidationMessage = LocalizationHelper.Translate("OnCall_Validation_TooLong");
                return;
            }

            // Validera ersättningsinställningar
            if (_activeJob.OnCallStandbyPayType == OnCallStandbyPayType.None)
            {
                // ok om det är None, men om du vill kräva att det är satt kan du stoppa här
            }
            else
            {
                if (_activeJob.OnCallStandbyPayAmount < 0)
                {
                    ValidationMessage = LocalizationHelper.Translate("OnCall_Validation_InvalidStandbyPay");
                    return;
                }
            }

            if (_activeJob.OnCallActivePayMode == OnCallActivePayMode.CustomHourly &&
                _activeJob.OnCallActiveHourlyRate <= 0)
            {
                ValidationMessage = LocalizationHelper.Translate("OnCall_Validation_InvalidActiveRate");
                return;
            }

            // Validera callouts (inkallningar)
            foreach (var c in Callouts)
            {
                var start = c.Start;
                var end = c.End;

                // över midnatt ok
                if (end <= start)
                    end = end.Add(TimeSpan.FromDays(1));

                var duration = end - start;
                var hours = (decimal)duration.TotalHours;

                if (hours <= 0)
                {
                    ValidationMessage = LocalizationHelper.Translate("OnCall_Validation_CalloutZero");
                    return;
                }

                if (hours > 24)
                {
                    ValidationMessage = LocalizationHelper.Translate("OnCall_Validation_CalloutTooLong");
                    return;
                }
            }

            // Total aktiv tid får inte vara större än standby (rimlig regel)
            var activeTotal = GetTotalActiveHoursFromCallouts();
            if (activeTotal > standbyHours)
            {
                ValidationMessage = LocalizationHelper.Translate("OnCall_Validation_ActiveTooLong");
                return;
            }

        }

        /// <summary>
        /// Triggar OnPropertyChanged för alla properties som beror på språk/valuta.
        /// Används när t.ex. språk eller valuta ändras.
        /// </summary>
        private void NotifyLocalizedProperties()
        {
            OnPropertyChanged(nameof(OnCallExplanationText));
            OnPropertyChanged(nameof(OnCallTotalsText));
        }

        private decimal GetCalloutHours(CalloutRow c)
        {
            if (c == null)
                return 0m;

            var s = c.Start;
            var e = c.End;

            if (e == s)
                return 0m;

            if (e <= s)
                e = e.Add(TimeSpan.FromDays(1));

            return (decimal)(e - s).TotalHours;
        }
        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            if (!_disposed)
            {
                LocalizationHelper.LanguageChanged -= OnLanguageChanged;
                _disposed = true;
            }
        }

        #endregion

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CalloutRow : INotifyPropertyChanged
    {
        private TimeSpan _start;
        private TimeSpan _end;
        private string? _notes;

        public TimeSpan Start
        {
            get => _start;
            set
            {
                if (_start == value)
                    return;
                _start = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TimeRangeText));
                OnPropertyChanged(nameof(HoursText));
            }
        }

        public TimeSpan End
        {
            get => _end;
            set
            {
                if (_end == value)
                    return;
                _end = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TimeRangeText));
                OnPropertyChanged(nameof(HoursText));
            }
        }

        public string? Notes
        {
            get => _notes;
            set
            {
                if (_notes == value)
                    return;
                _notes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasNotes));
            }
        }

        public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

        // UI-texter
        public string TimeRangeText => $"{Start:hh\\:mm} - {End:hh\\:mm}";
        public string HoursText => $"{GetHours():0.##}h";

        private decimal GetHours()
        {
            var s = Start;
            var e = End;
            if (e == s)
                return 0m;
            if (e <= s)
                e = e.Add(TimeSpan.FromDays(1));
            return (decimal)(e - s).TotalHours;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
