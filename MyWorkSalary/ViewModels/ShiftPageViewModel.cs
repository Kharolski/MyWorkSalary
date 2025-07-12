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
        private readonly ISickLeaveRepository _sickLeaveRepository;
        private JobProfile _activeJob;
        private ObservableCollection<WorkShift> _workShifts;
        #endregion

        #region Constructor
        public ShiftPageViewModel(
            IJobProfileRepository jobProfileRepository,
            IWorkShiftRepository workShiftRepository,
            ISickLeaveRepository sickLeaveRepository)
        {
            _jobProfileRepository = jobProfileRepository;
            _workShiftRepository = workShiftRepository;
            _sickLeaveRepository = sickLeaveRepository;

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

            // Ladda pass för aktivt jobb
            if (_activeJob != null)
            {
                // ANVÄNDER REPOSITORY METOD
                var shifts = _workShiftRepository.GetWorkShifts(_activeJob.Id)
                                               .OrderByDescending(s => s.ShiftDate);

                // Gruppering
                var grouped = shifts.GroupBy(s => GetMonthYearKey(s))
                                   .Select(g => new GroupedWorkShift(g.Key, g))
                                   .ToList();

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
                    // TODO: När VABRepository är klart
                    // var vabLeave = _vabRepository.GetVABByWorkShiftId(shift.Id);
                    // if (vabLeave != null) _vabRepository.DeleteVAB(vabLeave.Id);
                    break;

                case ShiftType.Vacation:
                    // TODO: När VacationRepository är klart
                    // var vacation = _vacationRepository.GetVacationByWorkShiftId(shift.Id);
                    // if (vacation != null) _vacationRepository.DeleteVacation(vacation.Id);
                    break;

                case ShiftType.OnCall:
                    // TODO: När OnCallRepository är klart
                    // var onCall = _onCallRepository.GetOnCallByWorkShiftId(shift.Id);
                    // if (onCall != null) _onCallRepository.DeleteOnCall(onCall.Id);
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
                ShiftType.Vacation =>
                    $"Vill du radera semestern från {dateStr}?\n({shift.NumberOfDays} dagar)",
                ShiftType.SickLeave =>
                    $"Vill du radera sjukskrivningen från {dateStr}?\n({shift.NumberOfDays} dagar)",
                ShiftType.VAB =>
                    $"Vill du radera VAB från {dateStr}?\n({shift.NumberOfDays} dagar)",
                ShiftType.OnCall =>
                    $"Vill du radera jourpasset från {dateStr}?",
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

    public class GroupedWorkShift : List<WorkShift>
    {
        public string MonthYear { get; private set; }
        public decimal TotalHours { get; private set; }

        // Visa bara timmar
        public string HoursSummary => $"{TotalHours:F1}t";

        public GroupedWorkShift(string monthYear, IEnumerable<WorkShift> shifts) : base(shifts)
        {
            MonthYear = monthYear;

            // Räkna arbetstimmar: Regular, Vacation, OnCall
            TotalHours = this.Where(s => s.ShiftType == ShiftType.Regular ||
                                        s.ShiftType == ShiftType.Vacation ||
                                        s.ShiftType == ShiftType.OnCall)
                            .Sum(s => s.TotalHours);
        }

    }
}
