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
            VacationType vacationType, decimal semesterKvot = 1.0m)
        {
            try
            {
                // Validering
                if (jobProfile == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Inget jobb angivet");
                    return false;
                }

                // Timanställd kan inte ha betald semester
                if (vacationType == VacationType.PaidVacation &&
                    jobProfile.EmploymentType == EmploymentType.Temporary)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Timanställd kan inte ha betald semester");
                    return false;
                }

                // Kontrollera återstående semesterdagar med kvot
                if (vacationType == VacationType.PaidVacation)
                {
                    var remainingDays = await GetRemainingVacationDays(jobProfile.Id);
                    if (remainingDays < semesterKvot)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Inte tillräckligt med semesterdagar kvar ({remainingDays:F1})");
                        return false;
                    }
                }

                // Skapa WorkShift utan beräkningar
                var workShift = new WorkShift
                {
                    JobProfileId = jobProfile.Id,
                    ShiftDate = vacationDate,
                    ShiftType = ShiftType.Vacation,
                    StartTime = null,
                    EndTime = null,
                    TotalHours = 8.0m,  // Standard arbetsdag
                    TotalPay = 0,       // Beräknas i rapporten!
                    Notes = GetVacationNote(vacationType, semesterKvot)
                };

                // Spara WorkShift
                var savedWorkShift = await _workShiftRepository.SaveWorkShiftAsync(workShift);

                // Skapa VacationLeave utan beräkningar
                var vacationLeave = new VacationLeave
                {
                    WorkShiftId = savedWorkShift.Id,
                    VacationType = vacationType,
                    VacationDaysUsed = 1.0m,  // Alltid 1 dag
                    VacationHours = 8.0m,     
                    TotalVacationDaysPerYear = 25m,
                    SemesterKvot = semesterKvot,
                    VacationDaysConsumed = semesterKvot

                };

                // Spara VacationLeave
                await _vacationLeaveRepository.InsertAsync(vacationLeave);

                System.Diagnostics.Debug.WriteLine($"✅ Semester sparad: {vacationType} för {vacationDate:yyyy-MM-dd}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Fel i SaveSimpleVacation: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"❌ Fel i GetRemainingVacationDays: {ex.Message}");
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
        private string GetVacationNote(VacationType vacationType, decimal kvot = 1.0m)
        {
            var kvotText = kvot != 1.0m ? $" (kvot: {kvot})" : "";

            return vacationType switch
            {
                VacationType.PaidVacation => $"Betald semester - 1 dag{kvotText}",
                VacationType.UnpaidVacation => $"Obetald ledighet - 1 dag{kvotText}",
                _ => $"Semester - 1 dag{kvotText}"
            };
        }

        #endregion
    }
}
