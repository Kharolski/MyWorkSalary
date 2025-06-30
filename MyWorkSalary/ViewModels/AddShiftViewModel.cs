using MyWorkSalary.Models;
using MyWorkSalary.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    //[QueryProperty(nameof(JobId), "jobId")]
    public class AddShiftViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly DatabaseService _databaseService;
        private JobProfile _activeJob;
        private DateTime _selectedDate = DateTime.Today;
        private TimeSpan _startTime = new TimeSpan(8, 0, 0); // 08:00
        private TimeSpan _endTime = new TimeSpan(16, 0, 0);  // 16:00
        private ShiftType _selectedShiftType = ShiftType.Regular;
        private string _notes = string.Empty;
        private bool _showCalculation;
        private bool _canSave;
        private decimal _calculatedHours;
        private decimal _calculatedPay;
        #endregion

        #region Constructor
        public AddShiftViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;

            // Initiera listor
            ShiftTypes = new ObservableCollection<ShiftType>
            {
                ShiftType.Regular,
                ShiftType.Overtime,
                ShiftType.OnCall,
                ShiftType.Training
            };

            // Commands
            SaveCommand = new Command(OnSave, CanExecuteSave);
            CancelCommand = new Command(OnCancel);

            // Ladda aktivt jobb
            LoadActiveJob();

            // Beräkna initial
            CalculateHours();
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
                OnPropertyChanged(nameof(ActiveJobTitle));
            }
        }

        public string ActiveJobTitle => ActiveJob?.JobTitle ?? "Inget aktivt jobb";

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
                CalculateHours();
            }
        }

        public TimeSpan StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged();
                CalculateHours();
            }
        }

        public TimeSpan EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged();
                CalculateHours();
            }
        }

        public ShiftType SelectedShiftType
        {
            get => _selectedShiftType;
            set
            {
                _selectedShiftType = value;
                OnPropertyChanged();
                CalculateHours();
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

        public ObservableCollection<ShiftType> ShiftTypes { get; }

        public bool ShowCalculation
        {
            get => _showCalculation;
            set
            {
                _showCalculation = value;
                OnPropertyChanged();
            }
        }

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

        public decimal CalculatedHours
        {
            get => _calculatedHours;
            set
            {
                _calculatedHours = value;
                OnPropertyChanged();
            }
        }

        public decimal CalculatedPay
        {
            get => _calculatedPay;
            set
            {
                _calculatedPay = value;
                OnPropertyChanged();
            }
        }

        public bool ShowPay => ActiveJob?.HourlyRate > 0 && CalculatedPay > 0;

        //public string JobId { get; set; }
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
                var jobs = _databaseService.GetJobProfiles();
                ActiveJob = jobs.FirstOrDefault(j => j.IsActive);

                if (ActiveJob == null)
                {
                    // Ingen aktivt jobb - detta borde inte hända
                    Shell.Current.DisplayAlert("Fel", "Inget aktivt jobb hittades.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fel i LoadActiveJob: {ex.Message}");
            }
        }

        private void CalculateHours()
        {
            try
            {
                // Skapa DateTime för start och slut
                var startDateTime = SelectedDate.Add(StartTime);
                var endDateTime = SelectedDate.Add(EndTime);

                // Om sluttid är tidigare än starttid, lägg till en dag
                if (EndTime < StartTime)
                {
                    endDateTime = endDateTime.AddDays(1);
                }

                // Beräkna timmar
                var duration = endDateTime - startDateTime;
                CalculatedHours = (decimal)duration.TotalHours;

                // Beräkna grundläggande lön (om timlön finns)
                if (ActiveJob?.HourlyRate > 0)  
                {
                    CalculatedPay = CalculatedHours * ActiveJob.HourlyRate.Value;  
                }
                else
                {
                    CalculatedPay = 0;
                }

                // Visa beräkning om det finns timmar
                ShowCalculation = CalculatedHours > 0;

                // Validering
                CanSave = CalculatedHours > 0 && CalculatedHours <= 24 && ActiveJob != null;

                OnPropertyChanged(nameof(ShowPay));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fel i CalculateHours: {ex.Message}");
                ShowCalculation = false;
                CanSave = false;
            }
        }

        private bool CanExecuteSave()
        {
            return CanSave;
        }

        private async void OnSave()
        {
            if (ActiveJob == null)
            {
                await Shell.Current.DisplayAlert("Fel", "Inget aktivt jobb.", "OK");
                return;
            }

            try
            {
                // Skapa WorkShift objekt
                var startDateTime = SelectedDate.Add(StartTime);
                var endDateTime = SelectedDate.Add(EndTime);
                if (EndTime < StartTime)
                {
                    endDateTime = endDateTime.AddDays(1);
                }

                var workShift = new WorkShift
                {
                    JobProfileId = ActiveJob.Id,
                    StartTime = startDateTime,
                    EndTime = endDateTime,
                    ShiftType = SelectedShiftType,
                    TotalHours = CalculatedHours,
                    RegularHours = CalculatedHours, // Förenklad - senare kan vi dela upp OB
                    OBHours = 0, // TODO: Beräkna OB-timmar
                    RegularPay = CalculatedPay,
                    OBPay = 0, // TODO: Beräkna OB-tillägg
                    TotalPay = CalculatedPay,
                    Notes = Notes,
                    CreatedDate = DateTime.Now,
                    IsConfirmed = false
                };

                // ANVÄND NY VALIDERING ISTÄLLET FÖR DIREKT SPARANDE
                var result = await _databaseService.SaveWorkShiftWithValidation(workShift);

                if (result.Success)
                {
                    // Framgång - spara lyckades
                    await Shell.Current.DisplayAlert(
                        "Sparat!",
                        $"Arbetspass på {CalculatedHours:F1} timmar har sparats.",
                        "OK");

                    // Gå tillbaka
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    // Överlapp upptäckt - visa felmeddelande med alternativ
                    bool changeTime = await Shell.Current.DisplayAlert(
                        "⚠️ Överlappande pass",
                        result.Message,
                        "Ändra tiden",
                        "Avbryt");

                    if (!changeTime)
                    {
                        // Användaren valde "Avbryt" - gå tillbaka utan att spara
                        await Shell.Current.GoToAsync("..");
                    }
                    // Om "Ändra tiden" - stanna kvar i formuläret så användaren kan justera
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Fel",
                    $"Kunde inte spara passet: {ex.Message}",
                    "OK");
            }
        }

        private async void OnCancel()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Avbryt",
                "Vill du avbryta utan att spara?",
                "Ja, avbryt",
                "Fortsätt redigera");

            if (confirm)
            {
                await Shell.Current.GoToAsync("..");
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
}