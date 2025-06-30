using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using MyWorkSalary.Models;
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
            var jobs = _databaseService.GetJobProfiles();
            _activeJob = jobs.FirstOrDefault(j => j.IsActive);
            OnPropertyChanged(nameof(ActiveJobTitle));

            // Ladda pass för aktivt jobb
            if (_activeJob != null)
            {
                var shifts = _databaseService.GetWorkShifts(_activeJob.Id)
                                           .OrderByDescending(s => s.StartTime);

                // Gruppera per månad
                var grouped = shifts.GroupBy(s => s.StartTime.ToString("MMMM yyyy", new System.Globalization.CultureInfo("sv-SE")))
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

        private async void OnDeleteShift(WorkShift shift)
        {
            if (shift == null)
                return;

            // Bekräfta radering
            bool confirm = await Shell.Current.DisplayAlert(
                "Radera pass",
                $"Vill du radera passet från {shift.StartTime:dd/MM HH:mm} till {shift.EndTime:HH:mm}?",
                "Radera",
                "Avbryt");

            if (confirm)
            {
                try
                {
                    // Radera från databas
                    _databaseService.DeleteWorkShift(shift.Id);

                    // Uppdatera UI
                    LoadData();

                    //await Shell.Current.DisplayAlert("Klart", "Passet har raderats", "OK");
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Fel", $"Kunde inte radera passet: {ex.Message}", "OK");
                }
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

    public class GroupedWorkShift : List<WorkShift>
    {
        public string MonthYear { get; private set; }
        public decimal TotalHours { get; private set; }

        public GroupedWorkShift(string monthYear, IEnumerable<WorkShift> shifts) : base(shifts)
        {
            MonthYear = monthYear;
            TotalHours = this.Sum(s => s.TotalHours);
        }
    }
}
