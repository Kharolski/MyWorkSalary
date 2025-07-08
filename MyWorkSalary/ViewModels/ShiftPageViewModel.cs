using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services;
using MyWorkSalary.Views.Pages;

namespace MyWorkSalary.ViewModels
{
    public class ShiftPageViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly DatabaseService _databaseService;
        private JobProfile _activeJob;
        private ObservableCollection<WorkShift> _workShifts;
        #endregion

        #region Constructor
        public ShiftPageViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;

            // Commands
            AddShiftCommand = new Command(OnAddShift);
            DeleteShiftCommand = new Command<WorkShift>(OnDeleteShift);

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
            // Ladda aktivt jobb
            var jobs = _databaseService.JobProfiles.GetJobProfiles();
            _activeJob = jobs.FirstOrDefault(j => j.IsActive);
            OnPropertyChanged(nameof(ActiveJobTitle));

            // Ladda pass för aktivt jobb
            if (_activeJob != null)
            {
                var shifts = _databaseService.WorkShifts.GetWorkShifts(_activeJob.Id)
                                           .OrderByDescending(s => s.ShiftDate);

                // 🔍 DEBUG - Visa ALLA pass
                System.Diagnostics.Debug.WriteLine($"🔍 LoadData Debug:");
                System.Diagnostics.Debug.WriteLine($"   ActiveJob ID: {_activeJob.Id}");
                System.Diagnostics.Debug.WriteLine($"   Totalt antal pass: {shifts.Count()}");

                foreach (var shift in shifts)
                {
                    System.Diagnostics.Debug.WriteLine($"   Pass: {shift.ShiftDate:yyyy-MM-dd} | Typ: {shift.ShiftType} | Timmar: {shift.TotalHours}");
                }

                // Gruppering
                var grouped = shifts.GroupBy(s => GetMonthYearKey(s))
                                   .Select(g => new GroupedWorkShift(g.Key, g))
                                   .ToList();

                System.Diagnostics.Debug.WriteLine($"🔍 Gruppering:");
                System.Diagnostics.Debug.WriteLine($"   Antal grupper: {grouped.Count}");
                foreach (var group in grouped)
                {
                    System.Diagnostics.Debug.WriteLine($"   Grupp: {group.MonthYear} | Pass: {group.Count} | Timmar: {group.TotalHours}");
                }

                GroupedWorkShifts = new ObservableCollection<GroupedWorkShift>(grouped);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Inget aktivt jobb!");
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

            // Ampassat bekräftelsemeddelande baserat på passtyp
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
                    // Radera från databas
                    _databaseService.WorkShifts.DeleteWorkShift(shift.Id);

                    // Uppdatera UI
                    LoadData();

                    // bekräftelsemeddelande
                    string deletedMessage = GetDeletedMessage(shift);
                    await Shell.Current.DisplayAlert("Raderat", deletedMessage, "OK");
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Fel", $"Kunde inte radera passet: {ex.Message}", "OK");
                }
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

                //ShiftType.Training =>
                //    shift.NumberOfDays.HasValue
                //        ? $"Vill du radera utbildningen från {dateStr}?\n({shift.NumberOfDays} dagar)"
                //        : $"Vill du radera utbildningspasset från {dateStr}?\n({shift.StartTime:HH:mm} - {shift.EndTime:HH:mm})",

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
                //ShiftType.Training => "Utbildningen har raderats",
                ShiftType.OnCall => "Jourpasset har raderats",
                //ShiftType.Overtime => "Övertidspasset har raderats",
                _ => "Passet har raderats"
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

    public class GroupedWorkShift : List<WorkShift>
    {
        public string MonthYear { get; private set; }
        public decimal TotalHours { get; private set; }

        // visa bara timmar
        public string HoursSummary => $"{TotalHours:F1}h";

        public GroupedWorkShift(string monthYear, IEnumerable<WorkShift> shifts) : base(shifts)
        {
            MonthYear = monthYear;

            // Fokus på arbetstimmar
            TotalHours = this.Where(s => s.ShiftType != ShiftType.Vacation &&
                                        s.ShiftType != ShiftType.SickLeave)
                            .Sum(s => s.TotalHours);
        }
    }
}
