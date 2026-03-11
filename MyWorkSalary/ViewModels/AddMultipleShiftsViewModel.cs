using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public class AddMultipleShiftsViewModel : BaseViewModel
    {
        #region Private Fields
        private readonly IJobProfileRepository _jobProfileRepository;
        private readonly IWorkShiftRepository _workShiftRepository;

        private JobProfile _activeJob;
        private TimeSpan _startTime;
        private TimeSpan _endTime;
        private int _selectedDaysCount;
        private bool _canSave;
        private DateTime _currentMonth;
        #endregion

        #region Constructor
        public AddMultipleShiftsViewModel(
            IJobProfileRepository jobProfileRepository,
            IWorkShiftRepository workShiftRepository)
        {
            _jobProfileRepository = jobProfileRepository;
            _workShiftRepository = workShiftRepository;

            // Initiera värden
            _currentMonth = DateTime.Today;
            _startTime = new TimeSpan(8, 0, 0); // 08:00
            _endTime = new TimeSpan(16, 0, 0);  // 16:00

            // Commands
            SaveCommand = new Command(OnSave, CanExecuteSave);
            CancelCommand = new Command(OnCancel);

            LoadActiveJob();
        }
        #endregion

        #region Properties
        public JobProfile ActiveJob
        {
            get => _activeJob;
            set
            {
                _activeJob = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged();
                // TODO: Kör validation här senare
            }
        }

        public TimeSpan EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged();
                // TODO: Kör validation här senare
            }
        }

        public int SelectedDaysCount
        {
            get => _selectedDaysCount;
            set
            {
                _selectedDaysCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedDaysText));
            }
        }

        public string SelectedDaysText => $"Selected days: {SelectedDaysCount}";

        public bool CanSave
        {
            get => _canSave;
            set
            {
                _canSave = value;
                OnPropertyChanged();
                ((Command)SaveCommand).ChangeCanExecute();
            }
        }

        public DateTime CurrentMonth
        {
            get => _currentMonth;
            set
            {
                _currentMonth = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MonthText));
                // TODO: Reset valda dagar här senare
            }
        }

        public string MonthText => CurrentMonth.ToString("MMMM yyyy");
        #endregion

        #region Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        #region Methods
        private void LoadActiveJob()
        {
            try
            {
                ActiveJob = _jobProfileRepository.GetActiveJob();
                if (ActiveJob == null)
                {
                    // TODO: Hantera inget aktivt jobb
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading active job: {ex.Message}");
            }
        }

        private bool CanExecuteSave()
        {
            return CanSave && SelectedDaysCount > 0 && ActiveJob != null;
        }

        private async void OnSave()
        {
            // TODO: Implementera sparning
            await Shell.Current.DisplayAlert("Info", "Sparning kommer implementeras senare", "OK");
        }

        private async void OnCancel()
        {
            await Shell.Current.GoToAsync("..");
        }
        #endregion
    }
}