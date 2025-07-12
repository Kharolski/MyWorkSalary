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
        #endregion

        #region Constructor
        public VacationHandler(
            IWorkShiftRepository workShiftRepository,
            IVacationLeaveRepository vacationLeaveRepository)
        {
            _workShiftRepository = workShiftRepository;
            _vacationLeaveRepository = vacationLeaveRepository;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Spara semester utan beräkningar
        /// </summary>
        public async Task<bool> SaveSimpleVacation(
            DateTime vacationDate,
            JobProfile jobProfile,
            VacationType vacationType, decimal semesterKvot = 1.0m, 
            decimal plannedWorkHours = 0m)
        {
            try
            {
                // Validering
                if (jobProfile == null)
                {
                    LogDebug("❌ Inget jobb angivet");
                    return false;
                }

                // Timanställd kan inte ha betald semester
                if (vacationType == VacationType.PaidVacation &&
                    jobProfile.EmploymentType == EmploymentType.Temporary)
                {
                    LogDebug("❌ Timanställd kan inte ha betald semester");
                    return false;
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

                // Spara WorkShift
                var savedWorkShift = await _workShiftRepository.SaveWorkShiftAsync(workShift);

                if (savedWorkShift == null)
                {
                    LogDebug("❌ SaveWorkShiftAsync returnerade null!");
                    return false;
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

                return true;
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Fel i SaveSimpleVacation: {ex.Message}");
                return false;
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

                var usedDays = await _vacationLeaveRepository.GetTotalVacationDaysUsedAsync(jobProfileId, year);
                return Math.Max(0, 25m - usedDays);
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Fel i GetRemainingVacationDays: {ex.Message}");
                return 25m; // Returnera max om fel
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
                    return (false, "Inget jobb angivet");

                // Timanställd kan inte ha betald semester
                if (vacationType == VacationType.PaidVacation &&
                    jobProfile.EmploymentType == EmploymentType.Temporary)
                    return (false, "Timanställd kan inte ha betald semester - välj 'Obetald ledighet'");

                // Kontrollera återstående dagar för betald semester
                if (vacationType == VacationType.PaidVacation)
                {
                    var remainingDays = await GetRemainingVacationDays(jobProfile.Id);
                    if (remainingDays < 1.0m)
                        return (false, $"Inte tillräckligt med semesterdagar kvar ({remainingDays:F1} dagar)");
                }

                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, $"Fel vid validering: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Skapa anteckning baserat på semestertyp
        /// </summary>
        private string GetVacationNote(VacationType vacationType, decimal kvot = 1.0m, decimal plannedWorkHours = 0m)
        {
            var kvotText = kvot != 1.0m ? $" (kvot: {kvot})" : "";
            var plannedText = plannedWorkHours > 0 ? $" (skulle arbetat: {plannedWorkHours}t)" : "";

            return vacationType switch
            {
                VacationType.PaidVacation => $"Betald semester - 1 dag{kvotText}",
                VacationType.UnpaidVacation => $"Obetald ledighet - 1 dag{plannedText}",
                _ => $"Sem - 1 dag{kvotText}"
            };
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
