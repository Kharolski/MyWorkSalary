using MyWorkSalary.Helpers;
using MyWorkSalary.Helpers.Converters;
using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Services.Premium;
using MyWorkSalary.Views.Pages;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public class ShiftPageViewModel : BaseViewModel
    {
        #region Culture
        private CultureInfo AppCulture => TranslationManager.Instance.CurrentCulture;
        #endregion

        #region Private Fields
        private readonly IJobProfileRepository _jobProfileRepository;
        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly IVacationLeaveRepository _vacationLeaveRepository;
        private readonly ISickLeaveRepository _sickLeaveRepository;
        private readonly IOnCallRepository _onCallRepository;
        private readonly IOBEventRepository _obEventRepository;
        private readonly AdService _adService;

        private JobProfile _activeJob;
        private ObservableCollection<WorkShift> _workShifts;
        #endregion

        #region Constructor
        public ShiftPageViewModel(
            IJobProfileRepository jobProfileRepository,
            IWorkShiftRepository workShiftRepository,
            IVacationLeaveRepository vacationLeaveRepository,
            ISickLeaveRepository sickLeaveRepository,
            IOnCallRepository onCallRepository,
            IOBEventRepository obEventRepository,
            AdService adService)
        {
            _jobProfileRepository = jobProfileRepository;
            _workShiftRepository = workShiftRepository;
            _vacationLeaveRepository = vacationLeaveRepository;
            _sickLeaveRepository = sickLeaveRepository;
            _onCallRepository = onCallRepository;
            _obEventRepository = obEventRepository;
            _adService = adService;

            // Commands
            AddShiftCommand = new Command(OnAddShift);
            DeleteShiftCommand = new Command<WorkShift>(OnDeleteShift);
            CreateJobCommand = new Command(OnCreateJob);

            // Prenumerera på events
            ShiftToHoursDisplayConverter.SickLeaveDataUpdated += OnSickLeaveDataUpdated;
            ShiftToTimeStringConverter.SickLeaveDescriptionUpdated += OnSickLeaveDescriptionUpdated;

            IsBusy = true;

            // Ladda data
            LoadData();
        }
        #endregion

        #region Properties
        public string ActiveJobTitle => _activeJob?.JobTitle ?? LocalizationHelper.Translate("NoActiveJob");

        public string Workplace => _activeJob?.Workplace ?? "";
        public string SalaryDisplayText
        {
            get
            {
                if (_activeJob == null)
                    return "";

                // Fast anställd
                if (_activeJob.EmploymentType == EmploymentType.Permanent)
                {
                    return $"{_activeJob.SalaryDisplayText} • {LocalizationHelper.Translate("FlexTime")}";
                }

                // Timanställd / övrigt
                return $"{_activeJob.SalaryDisplayText} • {LocalizationHelper.Translate("HourlyWage")}";
            }
        }

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
        public bool NoShiftsVisible => HasActiveJob && !HasShifts;
        public bool HasActiveJob => _activeJob != null;
        public bool HasNoActiveJob => !HasActiveJob;
        #endregion

        #region Commands
        public ICommand AddShiftCommand { get; }
        public ICommand DeleteShiftCommand { get; }
        public ICommand CreateJobCommand { get; }
        #endregion

        #region Methods

        public async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;
                
                // Ladda data i bakgrunden för snabbare UI
                await Task.Run(() =>
                {
                    try
                    {
                        // Ladda aktivt jobb - ANVÄNDER REPOSITORY METOD
                        _activeJob = _jobProfileRepository.GetActiveJob();
                        ActiveJobProvider.Current = _activeJob;

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            OnPropertyChanged(nameof(ActiveJobTitle));
                            OnPropertyChanged(nameof(Workplace));
                            OnPropertyChanged(nameof(SalaryDisplayText));
                        });

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

                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                GroupedWorkShifts = new ObservableCollection<GroupedWorkShift>(grouped);
                            });
                        }
                        else
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                GroupedWorkShifts = new ObservableCollection<GroupedWorkShift>();
                            });
                        }
                    }
                    catch (Exception dataEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"🚨 ShiftPage data loading error: {dataEx}");
                        throw; // Kasta vidare för att hanteras i yttre catch
                    }
                });
                
                // Visa banner efter att data har laddats (om inte premium)
                try
                {
                    _adService.ShowBanner();
                }
                catch (Exception adEx)
                {
                    System.Diagnostics.Debug.WriteLine($"🚨 ShiftPage ad service error: {adEx}");
                    // Fortsätt även om banner misslyckas
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🚨 ShiftPage LoadDataAsync Error: {ex}");
                System.Diagnostics.Debug.WriteLine($"🚨 Stack Trace: {ex.StackTrace}");
                throw; // Kasta vidare för att hanteras i ShiftPage
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void LoadData()
        {
            _ = LoadDataAsync(); // Fire and forget för bakåtkompatibilitet
        }

        private async void OnAddShift()
        {
            if (_activeJob == null)
            {
                await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("NoJobAlertTitle"),
                    LocalizationHelper.Translate("NoJobAlertMessage"),
                    LocalizationHelper.Translate("Dialog_Ok"));
                return;
            }

            try
            {
                IsBusy = true;
                await Shell.Current.GoToAsync(nameof(AddShiftPage));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🚨 Navigate to AddShiftPage Error: {ex}");
                // Fortsätt även om navigering misslyckas
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Hantera radering av alla passtyper
        private async void OnDeleteShift(WorkShift shift)
        {
            if (shift == null)
                return;

            // Anpassat bekräftelsemeddelande baserat på passtyp
            string confirmMessage = GetDeleteConfirmationMessage(shift);
            bool confirm = await Shell.Current.DisplayAlert(
                LocalizationHelper.Translate("DeleteShiftTitle"),
                confirmMessage,
                LocalizationHelper.Translate("DeleteShiftButton"),
                LocalizationHelper.Translate("CancelButton"));

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
                    await Shell.Current.DisplayAlert(
                        LocalizationHelper.Translate("DeletedTitle"),
                        deletedMessage,
                        LocalizationHelper.Translate("Dialog_Ok"));
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert(
                        LocalizationHelper.Translate("DeleteError"),
                        string.Format(LocalizationHelper.Translate("DeleteErrorMessage"), ex.Message),
                        LocalizationHelper.Translate("Dialog_Ok"));
                }
            }
        }

        // Radera specialiserad data
        private async Task DeleteSpecializedData(WorkShift shift)
        {
            // Ta bort OB-händelser för detta pass
            try
            {
                _obEventRepository.DeleteForWorkShift(shift.Id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fel vid borttagning av OBEvent för WorkShift {shift.Id}: {ex.Message}");
                throw;
            }

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
                    
                    break;
            }
        }

        // Få rätt månad/år för gruppering
        private string GetMonthYearKey(WorkShift shift)
        {
            // Använd ShiftDate för alla passtyper
            return shift.ShiftDate.ToString("MMMM yyyy", AppCulture);
        }

        // Skapa bekräftelsemeddelande
        private string GetDeleteConfirmationMessage(WorkShift shift)
        {
            var dateStr = shift.ShiftDate.ToString("dddd d MMMM", AppCulture);

            if (shift.ShiftType == ShiftType.Regular && shift.StartTime.HasValue && shift.EndTime.HasValue)
            {
                return string.Format(
                    LocalizationHelper.Translate("DeleteShiftWithTimeConfirm"),
                    dateStr,
                    shift.StartTime?.ToString("t", AppCulture),  // t = kort tidformat (HH:mm)
                    shift.EndTime?.ToString("t", AppCulture));
            }

            var messageKey = shift.ShiftType switch
            {
                ShiftType.Vacation => "DeleteVacationConfirm",
                ShiftType.SickLeave => "DeleteSickLeaveConfirm",
                ShiftType.OnCall => "DeleteOnCallConfirm",
                _ => "DeleteShiftConfirm"
            };

            return string.Format(LocalizationHelper.Translate(messageKey), dateStr);
        }

        // Skapa raderingsbekräftelse
        private string GetDeletedMessage(WorkShift shift)
        {
            return shift.ShiftType switch
            {
                ShiftType.Vacation => LocalizationHelper.Translate("DeletedVacation"),
                ShiftType.SickLeave => LocalizationHelper.Translate("DeletedSickLeave"),
                ShiftType.OnCall => LocalizationHelper.Translate("DeletedOnCall"),
                _ => LocalizationHelper.Translate("DeletedShift")
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

        private async void OnCreateJob()
        {
            await Shell.Current.GoToAsync("//SettingsPage");
        }
        #endregion
    }

    public class GroupedWorkShift : List<WorkShift>, INotifyPropertyChanged
    {
        #region Notes Keys (Do not localize)
        private const string PlannedHoursKey = "PlannedHours:";
        #endregion

        #region Property
        private string HoursAbbr => LocalizationHelper.Translate("Hours_Abbreviation"); // ex "h" / "t"

        public string MonthYear { get; private set; }
        public decimal TotalHours { get; private set; }

        // Visa bara timmar
        public string HoursSummary
        {
            get
            {
                var sign = TotalHours > 0 ? "+" : "";
                return $"{sign}{TotalHours:0.##} {HoursAbbr}";
            }
        }
        #endregion


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
                case ShiftType.Vacation when shift.TotalHours <= 0:
                    // Obetald semester: Hämta planerade timmar och gör negativa
                    if (shift.Notes != null && shift.Notes.Contains(PlannedHoursKey))
                    {
                        var parts = shift.Notes.Split('|');
                        var plannedPart = parts.FirstOrDefault(p => p.StartsWith(PlannedHoursKey));
                        if (plannedPart != null)
                        {
                            var hoursText = plannedPart.Replace(PlannedHoursKey, "");
                            if (!decimal.TryParse(hoursText, NumberStyles.Number, CultureInfo.InvariantCulture, out var plannedHours) &&
                                !decimal.TryParse(hoursText, NumberStyles.Number, CultureInfo.CurrentCulture, out plannedHours))
                            {
                                return 0;
                            }
                            return -plannedHours; // Negativa timmar för obetald semester
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

        /// <summary>
        /// Räknar om TotalHours och HoursSummary för gruppen baserat på aktuella WorkShift-data.
        /// 
        /// OBS:
        /// Anropas INTE i dagsläget eftersom grupperna alltid byggs om via LoadData().
        /// Metoden finns för framtida scenarion där ett enskilt pass uppdateras,
        /// läggs till eller tas bort utan att hela listan behöver återskapas.
        /// 
        /// Exempel på framtida användning:
        /// - Uppdatering av VAB/Sjuk/Notes
        /// - Live-uppdatering av timmar i UI
        /// - Optimering för stora datamängder
        /// </summary>
        public void RecalculateTotals()
        {
            TotalHours = this.Sum(s => GetEffectiveHours(s));
            OnPropertyChanged(nameof(TotalHours));
            OnPropertyChanged(nameof(HoursSummary));
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

    }
}
