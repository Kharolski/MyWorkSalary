using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using MyWorkSalary.Helpers.Converters;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Views.Pages;

namespace MyWorkSalary.ViewModels
{
    public class ShiftPageViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly IJobProfileRepository _jobProfileRepository;
        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly IVABLeaveRepository _vabLeaveRepository;
        private readonly IVacationLeaveRepository _vacationLeaveRepository;
        private readonly ISickLeaveRepository _sickLeaveRepository;
        private readonly IOnCallRepository _onCallRepository;

        private JobProfile _activeJob;
        private ObservableCollection<WorkShift> _workShifts;
        #endregion

        #region Constructor
        public ShiftPageViewModel(
            IJobProfileRepository jobProfileRepository,
            IWorkShiftRepository workShiftRepository,
            IVABLeaveRepository vabLeaveRepository,
            IVacationLeaveRepository vacationLeaveRepository,
            ISickLeaveRepository sickLeaveRepository,
            IOnCallRepository onCallRepository)
        {
            _jobProfileRepository = jobProfileRepository;
            _workShiftRepository = workShiftRepository;
            _vabLeaveRepository = vabLeaveRepository;
            _vacationLeaveRepository = vacationLeaveRepository;
            _sickLeaveRepository = sickLeaveRepository;
            _onCallRepository = onCallRepository;

            // Commands
            AddShiftCommand = new Command(OnAddShift);
            DeleteShiftCommand = new Command<WorkShift>(OnDeleteShift);

            // Prenumerera på events
            ShiftToHoursDisplayConverter.SickLeaveDataUpdated += OnSickLeaveDataUpdated;
            ShiftToTimeStringConverter.SickLeaveDescriptionUpdated += OnSickLeaveDescriptionUpdated;

            // Ladda data
            LoadData();
        }
        #endregion

        #region Properties
        public string ActiveJobTitle => _activeJob?.JobTitle ?? "Inget aktivt jobb";

        public string Workplace => _activeJob?.Workplace ?? "";
        public string SalaryDisplayText => _activeJob?.ExpectedHoursPerMonth > 0
            ? $"{_activeJob.SalaryDisplayText:NO} • Flex-tid"
            : "Timlön";

        private ObservableCollection<GroupedWorkShift> _groupedWorkShifts;
        public ObservableCollection<GroupedWorkShift> GroupedWorkShifts
        {
            get => _groupedWorkShifts;
            set
            {
                _groupedWorkShifts = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasShifts));
                OnPropertyChanged(nameof(NoShiftsVisible));
            }
        }

        public ObservableCollection<WorkShift> WorkShifts
        {
            get => _workShifts;
            set
            {
                _workShifts = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasShifts));
                OnPropertyChanged(nameof(NoShiftsVisible));
            }
        }

        public bool HasShifts => GroupedWorkShifts?.Any() == true && GroupedWorkShifts.Any(g => g.Any());
        public bool NoShiftsVisible => !HasShifts;
        #endregion

        #region Commands
        public ICommand AddShiftCommand { get; }
        public ICommand DeleteShiftCommand { get; }
        #endregion

        #region Methods

        public void LoadData()
        {
            // Ladda aktivt jobb - ANVÄNDER REPOSITORY METOD
            _activeJob = _jobProfileRepository.GetActiveJob();
            OnPropertyChanged(nameof(ActiveJobTitle));
            OnPropertyChanged(nameof(Workplace));
            OnPropertyChanged(nameof(SalaryDisplayText));

            // Ladda pass för aktivt jobb
            if (_activeJob != null)
            {
                // ANVÄNDER REPOSITORY METOD
                var shifts = _workShiftRepository.GetWorkShifts(_activeJob.Id)
                                               .OrderByDescending(s => s.ShiftDate);

                // Gruppering med expand/collapse
                var grouped = shifts.GroupBy(s => GetMonthYearKey(s))
                                   .Select(g => new GroupedWorkShift(g.Key, g))
                                   .ToList();

                // första månaden Expanded
                for (int i = 0; i < grouped.Count; i++)
                {
                    grouped[i].IsExpanded = (i == 0);
                }

                GroupedWorkShifts = new ObservableCollection<GroupedWorkShift>(grouped);
            }
            else
            {
                GroupedWorkShifts = new ObservableCollection<GroupedWorkShift>();
            }
        }

        private async void OnAddShift()
        {
            if (_activeJob == null)
            {
                await Shell.Current.DisplayAlert("Inget jobb", "Du måste skapa ett jobb först i Inställningar.", "OK");
                return;
            }

            await Shell.Current.GoToAsync(nameof(AddShiftPage));
        }

        // Hantera radering av alla passtyper
        private async void OnDeleteShift(WorkShift shift)
        {
            if (shift == null)
                return;

            // Anpassat bekräftelsemeddelande baserat på passtyp
            string confirmMessage = GetDeleteConfirmationMessage(shift);
            bool confirm = await Shell.Current.DisplayAlert(
                "Radera pass",
                confirmMessage,
                "Radera",
                "Avbryt");

            if (confirm)
            {
                try
                {
                    // Radera specialiserad data först (om det finns)
                    await DeleteSpecializedData(shift);

                    // Radera WorkShift - ANVÄNDER REPOSITORY METOD
                    _workShiftRepository.DeleteWorkShift(shift.Id);

                    // Uppdatera UI
                    LoadData();

                    // Bekräftelsemeddelande
                    string deletedMessage = GetDeletedMessage(shift);
                    await Shell.Current.DisplayAlert("Raderat", deletedMessage, "OK");
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Fel", $"Kunde inte radera passet: {ex.Message}", "OK");
                }
            }
        }

        // Radera specialiserad data
        private async Task DeleteSpecializedData(WorkShift shift)
        {
            switch (shift.ShiftType)
            {
                case ShiftType.SickLeave:
                    // Hitta och radera SickLeave - ANVÄNDER REPOSITORY METOD
                    var sickLeave = _sickLeaveRepository.GetSickLeaveByWorkShiftId(shift.Id);
                    if (sickLeave != null)
                    {
                        _sickLeaveRepository.DeleteSickLeave(sickLeave.Id);
                    }
                    break;

                case ShiftType.VAB:
                    var vabLeave = await _vabLeaveRepository.GetByWorkShiftIdAsync(shift.Id);
                    if (vabLeave != null)
                    {
                        await _vabLeaveRepository.DeleteAsync(vabLeave.Id);
                    }
                    break;

                case ShiftType.Vacation:
                    // Hitta VacationLeave via WorkShift relation
                    var vacationLeaves = await _vacationLeaveRepository.GetByJobProfileAsync(shift.JobProfileId);
                    var vacationLeave = vacationLeaves.FirstOrDefault(v => v.WorkShiftId == shift.Id);
                    if (vacationLeave != null)
                    {
                        await _vacationLeaveRepository.DeleteAsync(vacationLeave.Id);
                    }
                    break;

                case ShiftType.OnCall:
                    var onCallShift = _onCallRepository.GetByWorkShiftId(shift.Id);
                    if (onCallShift != null)
                    {
                        _onCallRepository.Delete(onCallShift.Id);
                    }
                    break;

                case ShiftType.Regular:
                    // Regular shifts har ingen specialiserad data
                    break;
            }
        }

        // Få rätt månad/år för gruppering
        private string GetMonthYearKey(WorkShift shift)
        {
            var swedishCulture = new System.Globalization.CultureInfo("sv-SE");
            // Använd ShiftDate för alla passtyper
            return shift.ShiftDate.ToString("MMMM yyyy", swedishCulture);
        }

        // Skapa bekräftelsemeddelande
        private string GetDeleteConfirmationMessage(WorkShift shift)
        {
            var swedishCulture = new System.Globalization.CultureInfo("sv-SE");
            var dateStr = shift.ShiftDate.ToString("dddd d MMMM", swedishCulture);

            return shift.ShiftType switch
            {
                ShiftType.Vacation => $"Vill du radera semestern från {dateStr}?",
                ShiftType.SickLeave => $"Vill du radera sjukskrivningen från {dateStr}?",
                ShiftType.VAB => $"Vill du radera VAB från {dateStr}?",
                ShiftType.OnCall => $"Vill du radera jourpasset från {dateStr}?",
                _ => shift.StartTime.HasValue && shift.EndTime.HasValue
                    ? $"Vill du radera passet från {dateStr}?\n({shift.StartTime:HH:mm} - {shift.EndTime:HH:mm})"
                    : $"Vill du radera passet från {dateStr}?"
            };
        }

        // Skapa raderingsbekräftelse
        private string GetDeletedMessage(WorkShift shift)
        {
            return shift.ShiftType switch
            {
                ShiftType.Vacation => "Semestern har raderats",
                ShiftType.SickLeave => "Sjukskrivningen har raderats",
                ShiftType.VAB => "VAB har raderats",
                ShiftType.OnCall => "Jourpasset har raderats",
                _ => "Passet har raderats"
            };
        }

        private void OnSickLeaveDataUpdated(int workShiftId)
        {
            // Trigga UI-refresh
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadData(); 
            });
        }

        private void OnSickLeaveDescriptionUpdated(int workShiftId)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadData(); 
            });
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

    public class GroupedWorkShift : List<WorkShift>, INotifyPropertyChanged
    {
        public string MonthYear { get; private set; }
        public decimal TotalHours { get; private set; }

        // Visa bara timmar
        public string HoursSummary => $"{TotalHours:F1}t";

        #region Expand/Collapse
        private bool _isExpanded = true; // Default: öppen
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ExpandIcon));
            }
        }

        public ICommand ToggleExpandCommand => new Command(() => IsExpanded = !IsExpanded);

        // Pil-ikon
        public string ExpandIcon => IsExpanded ? "▼" : "▶";
        #endregion

        public GroupedWorkShift(string monthYear, IEnumerable<WorkShift> shifts) : base(shifts)
        {
            MonthYear = monthYear;

            // Räkna arbetstimmar: Regular, Vacation, OnCall
            TotalHours = this.Sum(s => GetEffectiveHours(s));
        }

        private decimal GetEffectiveHours(WorkShift shift)
        {
            switch (shift.ShiftType)
            {
                case ShiftType.VAB:
                    // VAB: Räkna bara jobbade timmar (inte förlorade)
                    if (shift.Notes != null && shift.Notes.StartsWith("VABData:"))
                    {
                        try
                        {
                            var data = shift.Notes.Replace("VABData:", "");
                            var parts = data.Split('|');
                            var workedPart = parts.FirstOrDefault(p => p.StartsWith("Worked="));

                            if (workedPart != null)
                            {
                                var worked = decimal.Parse(workedPart.Replace("Worked=", ""));
                                return worked; // Bara jobbade timmar
                            }
                        }
                        catch
                        {
                            // Fallback
                        }
                    }
                    return 0; // Fallback för VAB

                case ShiftType.Vacation when shift.TotalHours <= 0:
                    // Obetald semester: Hämta planerade timmar och gör negativa
                    if (shift.Notes != null && shift.Notes.Contains("PlannedHours:"))
                    {
                        var parts = shift.Notes.Split('|');
                        var plannedPart = parts.FirstOrDefault(p => p.StartsWith("PlannedHours:"));
                        if (plannedPart != null)
                        {
                            var hoursText = plannedPart.Replace("PlannedHours:", "");
                            if (decimal.TryParse(hoursText, out decimal plannedHours))
                            {
                                return -plannedHours;  // Returnera negativa timmar
                            }
                        }
                    }
                    return 0;

                case ShiftType.SickLeave:
                    return 0; // Sjukskrivning räknas inte som arbetstid

                default:
                    // Regular, Vacation (betald), OnCall
                    return shift.TotalHours;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
