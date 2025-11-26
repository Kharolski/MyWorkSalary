using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;
using System.Globalization;

namespace MyWorkSalary.Services.Handlers
{
    public class VABHandler
    {
        #region Private Fields
        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly IVABLeaveRepository _vabLeaveRepository;      
        private readonly IJobProfileRepository _jobProfileRepository;  
        #endregion

        #region Constructor
        public VABHandler(
            IWorkShiftRepository workShiftRepository,
            IVABLeaveRepository vabLeaveRepository,               
            IJobProfileRepository jobProfileRepository)           
        {
            _workShiftRepository = workShiftRepository;
            _vabLeaveRepository = vabLeaveRepository;
            _jobProfileRepository = jobProfileRepository;
        }
        #endregion

        #region Public Methods

        public async Task<ShiftHandlerResult> HandleVAB(DateTime date, JobProfile jobProfile, TimeSpan? startTime = null, TimeSpan? endTime = null, decimal scheduledHours = 8, decimal workedHours = 0)
        {
            try
            {
                // Kolla befintligt pass
                var existingShift = await CheckForExistingShift(date, jobProfile.Id);
                if (existingShift != null)
                {
                    return new ShiftHandlerResult
                    {
                        Success = false,
                        ShowConfirmationDialog = true,
                        ConfirmationMessage = string.Format(
                            LocalizationHelper.Translate("VAB_Confirm_ReplaceExistingShift"),
                            GetShiftTypeText(existingShift.ShiftType),
                            date.ToString("d MMM", CultureInfo.CurrentCulture)
                        ),
                        RequiresTimeInput = false
                    };
                }

                // Skapa WorkShift + VABLeave
                var (vabShift, vabLeave) = await CreateVABWithDetails(date, jobProfile, startTime, endTime, scheduledHours, workedHours);

                // Spara båda
                var savedShift = await SaveVABShift(vabShift);
                vabLeave.WorkShiftId = savedShift.Id;
                await _vabLeaveRepository.InsertAsync(vabLeave);

                return new ShiftHandlerResult
                {
                    Success = true,
                    Message = LocalizationHelper.Translate("VAB_Save_Success"),
                    CreatedShift = savedShift,
                    RequiresTimeInput = false
                };
            }
            catch (Exception ex)
            {
                return new ShiftHandlerResult
                {
                    Success = false,
                    Message = string.Format(
                        LocalizationHelper.Translate("VAB_Save_Error"),
                        ex.Message
                    )
                };
            }
        }


        public async Task<ShiftHandlerResult> ConfirmReplaceWithVAB(DateTime date, JobProfile jobProfile, decimal scheduledHours = 8, decimal workedHours = 0)
        {
            try
            {
                // 1. Ta bort befintligt pass OCH eventuell VABLeave
                await RemoveExistingShift(date, jobProfile.Id);

                // 2. Skapa nytt VAB
                var (vabShift, vabLeave) = await CreateVABWithDetails(date, jobProfile, null, null, scheduledHours, workedHours);
                var savedShift = await SaveVABShift(vabShift);
                vabLeave.WorkShiftId = savedShift.Id;
                await _vabLeaveRepository.InsertAsync(vabLeave);

                return new ShiftHandlerResult
                {
                    Success = true,
                    Message = LocalizationHelper.Translate("VAB_Confirm_ReplaceSuccess"),
                    CreatedShift = savedShift,
                    RequiresTimeInput = false
                };
            }
            catch (Exception ex)
            {
                return new ShiftHandlerResult
                {
                    Success = false,
                    Message = string.Format(
                        LocalizationHelper.Translate("VAB_Confirm_ReplaceError"),
                        ex.Message
                    )
                };
            }
        }
        #endregion

        #region Private Methods
        private async Task<WorkShift> CheckForExistingShift(DateTime date, int jobProfileId)
        {
            var shifts = _workShiftRepository.GetWorkShifts(jobProfileId);
            return shifts.FirstOrDefault(s => s.ShiftDate.Date == date.Date);
        }

        /// <summary>
        /// Skapar både WorkShift och VABLeave med korrekt beräkning
        /// </summary>
        private async Task<(WorkShift workShift, VABLeave vabLeave)> CreateVABWithDetails(
            DateTime date,
            JobProfile jobProfile,
            TimeSpan? startTime = null,
            TimeSpan? endTime = null,
            decimal scheduledHours = 8,
            decimal workedHours = 0)
        {
            // Bestäm VAB-typ
            var vabType = (workedHours > 0 && workedHours < scheduledHours) ? VABType.PartialDay : VABType.FullDay;
            decimal vabHours = scheduledHours - workedHours; // Förlorade timmar

            DateTime? startDateTime = null;
            DateTime? endDateTime = null;

            if (vabType == VABType.PartialDay && startTime.HasValue && endTime.HasValue)
            {
                startDateTime = date.Date.Add(startTime.Value);
                endDateTime = date.Date.Add(endTime.Value);
            }

            // Beräkna VAB-avdrag
            var (weeklyHours, hourlyRate, weeklyEarnings, vabDeduction, workedPay) =
                await CalculateVABPayment(jobProfile, vabHours, workedHours);

            // Skapa WorkShift
            var workShift = new WorkShift
            {
                JobProfileId = jobProfile.Id,
                ShiftDate = date,
                ShiftType = ShiftType.VAB,
                StartTime = startDateTime,
                EndTime = endDateTime,
                TotalHours = workedHours - scheduledHours, // Netto (negativt för VAB)
                RegularHours = workedHours,
                TotalPay = workedPay + vabDeduction,  // workedPay + (negativ vabDeduction)
                CreatedDate = DateTime.Now,
                IsConfirmed = true,

                // Används av converter att får data
                Notes = $"VABData:Scheduled={scheduledHours}|Worked={workedHours}|IsHourly={jobProfile.IsHourlyEmployee}"
            };

            // Skapa VABLeave
            var vabLeave = new VABLeave
            {
                VABType = vabType,
                WorkedStartTime = startTime,
                WorkedEndTime = endTime,
                ScheduledStartTime = TimeSpan.FromHours(8), // Default eller från schema
                ScheduledEndTime = TimeSpan.FromHours(8 + (double)scheduledHours),
                ScheduledHours = scheduledHours,
                WorkedHours = workedHours,
                VABHours = vabHours,
                WeeklyHoursUsed = weeklyHours,
                HourlyRateUsed = hourlyRate,
                WeeklyEarningsUsed = weeklyEarnings,
                WorkedPay = workedPay,
                VABDeduction = vabDeduction, // NEGATIV
                CreatedDate = DateTime.Now
            };

            return (workShift, vabLeave);
        }

        /// <summary>
        /// Beräknar VAB-betalning baserat på anställningstyp
        /// </summary>
        private async Task<(decimal weeklyHours, decimal hourlyRate, decimal weeklyEarnings, decimal vabDeduction, decimal workedPay)>
            CalculateVABPayment(JobProfile jobProfile, decimal vabHours, decimal workedHours)
        {
            if (jobProfile.IsHourlyEmployee)
            {
                // Timanställd - använd genomsnitt från 13 veckor
                var weeklyHours = await GetAverageWeeklyHours(jobProfile);
                var hourlyRate = jobProfile.HourlyRate ?? 0;
                var weeklyEarnings = weeklyHours * hourlyRate;

                var vabDeduction = -(vabHours * hourlyRate);  // NEGATIV
                var workedPay = workedHours * hourlyRate;     // POSITIV

                return (weeklyHours, hourlyRate, weeklyEarnings, vabDeduction, workedPay);
            }
            else
            {
                // Månadslönad - dagavdrag
                var monthlySalary = jobProfile.MonthlySalary ?? 0;
                var dailyDeduction = monthlySalary / 21; // Förenklad beräkning

                var vabDeduction = -(vabHours / 8 * dailyDeduction); // NEGATIV
                var workedPay = 0; // Månadslönad får ingen extra lön

                return (0, 0, monthlySalary, vabDeduction, workedPay);
            }
        }

        private async Task<decimal> GetAverageWeeklyHours(JobProfile jobProfile)
        {
            // Implementera logik för att hämta genomsnitt från senaste 13 veckor
            // Liknande som i SickLeaveHandler
            return 25; // Placeholder
        }

        private async Task<WorkShift> SaveVABShift(WorkShift vabShift)
        {
            return _workShiftRepository.SaveWorkShift(vabShift);
        }

        private async Task RemoveExistingShift(DateTime date, int jobProfileId)
        {
            var existingShift = await CheckForExistingShift(date, jobProfileId);
            if (existingShift != null)
            {
                // Ta bort VABLeave först (om det finns)
                var existingVAB = await _vabLeaveRepository.GetByWorkShiftIdAsync(existingShift.Id);
                if (existingVAB != null)
                {
                    await _vabLeaveRepository.DeleteAsync(existingVAB.Id);
                }

                // Ta bort WorkShift
                _workShiftRepository.DeleteWorkShift(existingShift.Id);
            }
        }

        private string GetShiftTypeText(ShiftType shiftType)
        {
            return shiftType switch
            {
                ShiftType.Regular => LocalizationHelper.Translate("ShiftType_RegularShift"),
                ShiftType.SickLeave => LocalizationHelper.Translate("ShiftType_SickLeaveShift"),
                ShiftType.Vacation => LocalizationHelper.Translate("ShiftType_VacationShift"),
                ShiftType.OnCall => LocalizationHelper.Translate("ShiftType_OnCallShift"),
                ShiftType.VAB => LocalizationHelper.Translate("ShiftType_VABShift"),
                _ => LocalizationHelper.Translate("ShiftType_Default")
            };
        }
        #endregion
    }
}
