using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;

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
        public async Task<ShiftHandlerResult> HandleVAB(DateTime date, JobProfile jobProfile, TimeSpan? startTime = null, TimeSpan? endTime = null)
        {
            try
            {
                // 1. Kolla befintligt pass
                var existingShift = await CheckForExistingShift(date, jobProfile.Id);
                if (existingShift != null)
                {
                    return new ShiftHandlerResult
                    {
                        Success = false,
                        ShowConfirmationDialog = true,
                        ConfirmationMessage = $"Du har redan ett {GetShiftTypeText(existingShift.ShiftType)} registrerat för {date:d MMM}.\n\nVill du ersätta det med VAB?",
                        RequiresTimeInput = false
                    };
                }

                // 2. Skapa WorkShift + VABLeave
                var (vabShift, vabLeave) = await CreateVABWithDetails(date, jobProfile, startTime, endTime);

                // 3. Spara båda
                var savedShift = await SaveVABShift(vabShift);
                vabLeave.WorkShiftId = savedShift.Id;
                await _vabLeaveRepository.InsertAsync(vabLeave);

                return new ShiftHandlerResult
                {
                    Success = true,
                    Message = "VAB registrerat",
                    CreatedShift = savedShift,
                    RequiresTimeInput = false
                };
            }
            catch (Exception ex)
            {
                return new ShiftHandlerResult
                {
                    Success = false,
                    Message = $"Fel vid VAB-registrering: {ex.Message}"
                };
            }
        }

        public async Task<ShiftHandlerResult> ConfirmReplaceWithVAB(DateTime date, JobProfile jobProfile)
        {
            try
            {
                // 1. Ta bort befintligt pass OCH eventuell VABLeave
                await RemoveExistingShift(date, jobProfile.Id);

                // 2. Skapa nytt VAB
                var (vabShift, vabLeave) = await CreateVABWithDetails(date, jobProfile);
                var savedShift = await SaveVABShift(vabShift);
                vabLeave.WorkShiftId = savedShift.Id;
                await _vabLeaveRepository.InsertAsync(vabLeave);

                return new ShiftHandlerResult
                {
                    Success = true,
                    Message = "Befintligt pass ersatt med VAB",
                    CreatedShift = savedShift,
                    RequiresTimeInput = false
                };
            }
            catch (Exception ex)
            {
                return new ShiftHandlerResult
                {
                    Success = false,
                    Message = $"Fel vid ersättning med VAB: {ex.Message}"
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
            TimeSpan? endTime = null)
        {
            // Bestäm VAB-typ och timmar
            var vabType = (startTime.HasValue && endTime.HasValue) ? VABType.PartialDay : VABType.FullDay;

            decimal scheduledHours = 8; // Default, eller hämta från schema
            decimal workedHours = 0;
            decimal vabHours = scheduledHours;

            DateTime? startDateTime = null;
            DateTime? endDateTime = null;

            if (vabType == VABType.PartialDay && startTime.HasValue && endTime.HasValue)
            {
                startDateTime = date.Date.Add(startTime.Value);
                endDateTime = date.Date.Add(endTime.Value);
                workedHours = (decimal)(endTime.Value - startTime.Value).TotalHours;
                vabHours = scheduledHours - workedHours;
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
                TotalHours = workedHours - vabHours, // Netto (kan vara negativt)
                RegularHours = workedHours,
                TotalPay = workedPay + vabDeduction,  // workedPay + (negativ vabDeduction)
                CreatedDate = DateTime.Now,
                IsConfirmed = true,
                Notes = $"VAB - {vabHours}t förlorade, {workedHours}t jobbade"
            };

            // Skapa VABLeave
            var vabLeave = new VABLeave
            {
                VABType = vabType,
                WorkedStartTime = startTime,
                WorkedEndTime = endTime,
                ScheduledStartTime = TimeSpan.FromHours(8), // Default eller från schema
                ScheduledEndTime = TimeSpan.FromHours(17),
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
                ShiftType.Regular => "arbetspass",
                ShiftType.SickLeave => "sjukdag",
                ShiftType.Vacation => "semesterdag",
                ShiftType.OnCall => "jourpass",
                ShiftType.VAB => "Vård av barn",
                _ => "pass"
            };
        }
        #endregion
    }
}
