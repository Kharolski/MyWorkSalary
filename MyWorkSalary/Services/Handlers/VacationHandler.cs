using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.Services.Handlers
{
    public class VacationHandler
    {
        #region Private Fields
        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly IVacationLeaveRepository _vacationLeaveRepository;
        private readonly IJobProfileRepository _jobProfileRepository;
        private readonly IWorkShiftService _workShiftService;
        #endregion

        #region Constructor
        public VacationHandler(
            IWorkShiftRepository workShiftRepository,
            IVacationLeaveRepository vacationLeaveRepository,
            IJobProfileRepository jobProfileRepository,
            IWorkShiftService workShiftService)
        {
            _workShiftRepository = workShiftRepository;
            _vacationLeaveRepository = vacationLeaveRepository;
            _jobProfileRepository = jobProfileRepository;
            _workShiftService = workShiftService;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Spara semester utan beräkningar
        /// </summary>
        public async Task<(bool Success, string Message)> SaveSimpleVacation(
            DateTime vacationDate,
            JobProfile jobProfile,
            VacationType vacationType,
            decimal semesterKvot = 1.0m,
            decimal plannedWorkHours = 0m)
        {
            try
            {
                // Validering
                if (jobProfile == null)
                {
                    return (false, LocalizationHelper.Translate("Error_NoJobProfile"));
                }

                // Timanställd kan inte ha betald semester
                if (vacationType == VacationType.PaidVacation && jobProfile.EmploymentType == EmploymentType.Temporary)
                {
                    return (false, LocalizationHelper.Translate("Vacation_NotAllowed_Temporary"));
                }

                // Bestäm TotalHours baserat på semestertyp
                decimal totalHours = vacationType switch
                {
                    VacationType.PaidVacation => 8.0m,      // Betald = 8h arbetstid
                    VacationType.UnpaidVacation => 0m,      // Obetald = 0h arbetstid
                    _ => 8.0m
                };

                // Skapa WorkShift
                var workShift = new WorkShift
                {
                    JobProfileId = jobProfile.Id,
                    ShiftDate = vacationDate,
                    ShiftType = ShiftType.Vacation,
                    StartTime = null,
                    EndTime = null,
                    TotalHours = totalHours,
                    TotalPay = 0,
                    Notes = GetVacationNote(vacationType, semesterKvot, plannedWorkHours)
                };

                // SPARA VIA WorkShiftService (med validering)
                var (success, message) = await _workShiftService.SaveWorkShiftWithValidation(workShift);
                if (!success)
                {
                    return (false, message);
                }

                // Hämta det sparade WorkShift för att få ID:t (via Repository)
                var allShifts = _workShiftRepository.GetWorkShiftsForDate(jobProfile.Id, vacationDate);
                var savedWorkShift = allShifts
                    .Where(x => x.ShiftType == ShiftType.Vacation)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault();

                if (savedWorkShift == null)
                {
                    return (false, LocalizationHelper.Translate("Vacation_WorkShiftNotFound"));
                }

                // Skapa VacationLeave
                var vacationLeave = new VacationLeave
                {
                    WorkShiftId = savedWorkShift.Id,
                    VacationType = vacationType,
                    VacationDaysUsed = 1.0m,
                    VacationHours = totalHours,
                    TotalVacationDaysPerYear = 25m,
                    SemesterKvot = semesterKvot,
                    VacationDaysConsumed = semesterKvot,
                    PlannedWorkHours = plannedWorkHours
                };

                // Spara VacationLeave
                await _vacationLeaveRepository.InsertAsync(vacationLeave);

                return (true, LocalizationHelper.Translate("Vacation_Saved"));
            }
            catch (Exception ex)
            {
                LogDebug($"❌ StackTrace: {ex.StackTrace}");
                return (false, LocalizationHelper.Translate("Error_SaveFailed", ex.Message));
            }
        }

        /// <summary>
        /// Hämta återstående semesterdagar för UI
        /// </summary>
        public async Task<decimal> GetRemainingVacationDays(int jobProfileId, int year = 0)
        {
            try
            {
                if (year == 0)
                    year = DateTime.Now.Year;

                // Hämta JobProfile (SYNKRON metod)
                var jobProfile = _jobProfileRepository.GetJobProfile(jobProfileId);
                if (jobProfile == null)
                {
                    LogDebug($"❌ Kunde inte hitta JobProfile med ID: {jobProfileId}");
                    return 25m;
                }

                // Totalt tillgängliga dagar = årskvot + sparade dagar från förra året
                var totalAvailable = jobProfile.VacationDaysPerYear + (jobProfile.InitialVacationBalance ?? 0);

                // Använda dagar detta år (bara betald semester)
                var usedDays = await _vacationLeaveRepository.GetTotalVacationDaysUsedAsync(jobProfileId, year);

                var remaining = Math.Max(0, totalAvailable - usedDays);

                return remaining;
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Fel i GetRemainingVacationDays: {ex.Message}");
                return 25m; // Fallback
            }
        }

        /// <summary>
        /// Validera om semester kan sparas
        /// </summary>
        public async Task<(bool CanSave, string ErrorMessage)> ValidateVacation(
            JobProfile jobProfile,
            VacationType vacationType)
        {
            try
            {
                if (jobProfile == null)
                    return (false, LocalizationHelper.Translate("Error_NoJobProfile"));

                // Timanställd kan inte ha betald semester
                if (vacationType == VacationType.PaidVacation &&
                    jobProfile.EmploymentType == EmploymentType.Temporary)
                    return (false, LocalizationHelper.Translate("Vacation_NotAllowed_Temporary_Detail"));

                // Kontrollera återstående dagar för betald semester
                if (vacationType == VacationType.PaidVacation)
                {
                    var remainingDays = await GetRemainingVacationDays(jobProfile.Id);
                    if (remainingDays < 1.0m)
                        return (false, LocalizationHelper.Translate("Vacation_InsufficientDays", remainingDays));
                }

                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, LocalizationHelper.Translate("Error_ValidationFailed", ex.Message));
            }
        }

        /// <summary>
        /// Validera om semester kan sparas på valt datum
        /// </summary>
        public async Task<(bool CanSave, string ErrorMessage, WorkShift ConflictingShift)> ValidateVacationDate(
            DateTime vacationDate,
            JobProfile jobProfile)
        {
            try
            {
                if (jobProfile == null)
                    return (false, LocalizationHelper.Translate("Error_NoJobProfile"), null);

                var existingShifts = await GetShiftsOnDate(jobProfile.Id, vacationDate);

                // 1. Kontrollera om det redan finns semester på detta datum
                var existingVacation = existingShifts.FirstOrDefault(s => s.ShiftType == ShiftType.Vacation);
                if (existingVacation != null)
                {
                    return (false, LocalizationHelper.Translate("Vacation_AlreadyRegistered"), existingVacation);
                }

                // 2. Kontrollera om det finns andra pass (arbete, sjuk, jour) på detta datum
                foreach (var shift in existingShifts)
                {
                    var conflictMessage = shift.ShiftType switch
                    {
                        ShiftType.Regular => LocalizationHelper.Translate(
                                    "Vacation_Conflict_Regular",
                                    shift.StartTime?.ToString("HH:mm"),
                                    shift.EndTime?.ToString("HH:mm")),

                        ShiftType.SickLeave => LocalizationHelper.Translate(
                                    "Vacation_Conflict_SickLeave",
                                    shift.NumberOfDays ?? 1),

                        ShiftType.OnCall => LocalizationHelper.Translate("Vacation_Conflict_OnCall"),

                        ShiftType.VAB => LocalizationHelper.Translate("Vacation_Conflict_VAB"),

                        _ => LocalizationHelper.Translate("Vacation_Conflict_Generic")
                    };

                    return (false, conflictMessage, shift);
                }

                return (true, "", null);
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Fel vid validering av semesterdatum: {ex.Message}");
                return (false, LocalizationHelper.Translate("Error_DateValidationFailed"), null);
            }
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Skapa anteckning baserat på semestertyp
        /// </summary>
        private string GetVacationNote(VacationType vacationType, decimal kvot = 1.0m, decimal plannedWorkHours = 0m)
        {
            var kvotText = kvot != 1.0m
                ? LocalizationHelper.Translate("Vacation_Note_Quota", kvot)
                : "";

            var plannedText = plannedWorkHours > 0
                ? LocalizationHelper.Translate("Vacation_Note_PlannedHours", plannedWorkHours)
                : "";

            return vacationType switch
            {
                VacationType.PaidVacation => LocalizationHelper.Translate("Vacation_Note_Paid", kvotText),
                VacationType.UnpaidVacation => LocalizationHelper.Translate(
                            "Vacation_Note_Unpaid",
                            kvotText,
                            plannedText),

                _ => LocalizationHelper.Translate("Vacation_Note_Generic", kvotText)
            };
        }

        /// <summary>
        /// Hämta alla pass på ett specifikt datum
        /// </summary>
        private async Task<List<WorkShift>> GetShiftsOnDate(int jobProfileId, DateTime date)
        {
            try
            {
                return await Task.FromResult(_workShiftRepository.GetWorkShiftsForDate(jobProfileId, date));
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Fel vid hämtning av pass för datum: {ex.Message}");
                return new List<WorkShift>();
            }
        }
        #endregion

        #region Debug Helper

        /// <summary>
        /// Debug-logging som fungerar på både emulator och riktig enhet
        /// Aktivera/inaktivera genom att ändra DEBUG_VACATION_HANDLER konstanten
        /// </summary>
        private const bool DEBUG_VACATION_HANDLER = false; // Sätt till true för debugging

        private void LogDebug(string message)
        {
            if (!DEBUG_VACATION_HANDLER)
                return;

#if ANDROID
            Android.Util.Log.Debug("VacationHandler", message);
#else
    System.Diagnostics.Debug.WriteLine(message);
#endif
        }
        #endregion
    }
}
