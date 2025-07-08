using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.Services.Handlers
{
    /// <summary>
    /// Hanterar VAB (Vård av barn) registrering och logik
    /// </summary>
    public class VABHandler
    {
        #region Private Fields
        private readonly DatabaseService _databaseService;
        #endregion

        #region Constructor
        /// <summary>
        /// Konstruktor för VABHandler
        /// </summary>
        /// <param name="databaseService">Databas service för att hämta/spara data</param>
        public VABHandler(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Hanterar VAB-registrering för given datum
        /// </summary>
        /// <param name="date">Datum för VAB</param>
        /// <param name="jobProfile">Jobbprofil för beräkningar</param>
        /// <returns>Resultat av VAB-hantering</returns>
        public async Task<ShiftHandlerResult> HandleVAB(DateTime date, JobProfile jobProfile, TimeSpan? startTime = null, TimeSpan? endTime = null)
        {
            try
            {
                // 1. Kolla om det redan finns ett pass denna dag
                var existingShift = await CheckForExistingShift(date, jobProfile.Id);

                if (existingShift != null)
                {
                    // Visa bekräftelsedialog
                    return new ShiftHandlerResult
                    {
                        Success = false,
                        ShowConfirmationDialog = true,
                        ConfirmationMessage = $"Du har redan ett {GetShiftTypeText(existingShift.ShiftType)} registrerat för {date:d MMM}.\n\nVill du ersätta det med VAB?",
                        RequiresTimeInput = false
                    };
                }

                // 2. Skapa VAB-pass
                var vabShift = await CreateVABShift(date, jobProfile, startTime, endTime);

                // 3. Spara i databas
                var savedShift = await SaveVABShift(vabShift);

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

        /// <summary>
        /// Bekräftar och ersätter befintligt pass med VAB
        /// </summary>
        /// <param name="date">Datum</param>
        /// <param name="jobProfile">Jobbprofil</param>
        /// <returns>Resultat av ersättning</returns>
        public async Task<ShiftHandlerResult> ConfirmReplaceWithVAB(DateTime date, JobProfile jobProfile)
        {
            try
            {
                // 1. Ta bort befintligt pass
                await RemoveExistingShift(date, jobProfile.Id);

                // 2. Skapa nytt VAB-pass (återanvänder HandleVAB logik)
                var vabShift = await CreateVABShift(date, jobProfile);
                var savedShift = await SaveVABShift(vabShift);

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
        /// <summary>
        /// Kollar om det finns befintligt pass för datum
        /// </summary>
        /// <param name="date">Datum att kolla</param>
        /// <param name="jobProfileId">Jobb-ID</param>
        /// <returns>Befintligt pass eller null</returns>
        private async Task<WorkShift> CheckForExistingShift(DateTime date, int jobProfileId)
        {
            var shifts = _databaseService.WorkShifts.GetWorkShifts(jobProfileId);
            return shifts.FirstOrDefault(s => s.ShiftDate.Date == date.Date);
        }

        /// <summary>
        /// Skapar VAB WorkShift objekt
        /// </summary>
        /// <param name="date">VAB-datum</param>
        /// <param name="jobProfile">Jobbprofil</param>
        /// <returns>VAB WorkShift</returns>
        private async Task<WorkShift> CreateVABShift(DateTime date, JobProfile jobProfile, TimeSpan? startTime = null, TimeSpan? endTime = null)
        {
            // Beräkna timmar om tider finns
            decimal totalHours = 0;
            DateTime? startDateTime = null;
            DateTime? endDateTime = null;

            if (!jobProfile.IsHourlyEmployee && startTime.HasValue && endTime.HasValue)
            {
                startDateTime = date.Date.Add(startTime.Value);
                endDateTime = date.Date.Add(endTime.Value);
                totalHours = -(decimal)(endTime.Value - startTime.Value).TotalHours;  
            }

            var vabShift = new WorkShift
            {
                JobProfileId = jobProfile.Id,
                ShiftDate = date,
                ShiftType = ShiftType.VAB,
                BreakMinutes = 0,
                NumberOfDays = 1,

                StartTime = startDateTime,
                EndTime = endDateTime,
                TotalHours = totalHours,  

                RegularHours = 0,
                OBHours = 0,
                RegularPay = 0,
                OBPay = 0,
                TotalPay = 0,
                SickPayPercentage = null,
                IsKarensDay = false,
                Notes = "VAB - Beräkning görs i ShiftCalculationService",
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                IsConfirmed = true
            };

            return vabShift;
        }

        /// <summary>
        /// Sparar VAB-pass i databas
        /// </summary>
        /// <param name="vabShift">VAB WorkShift att spara</param>
        /// <returns>Sparat WorkShift</returns>
        private async Task<WorkShift> SaveVABShift(WorkShift vabShift)
        {
            _databaseService.WorkShifts.SaveWorkShift(vabShift);
            return vabShift;
        }

        /// <summary>
        /// Tar bort befintligt pass
        /// </summary>
        /// <param name="date">Datum</param>
        /// <param name="jobProfileId">Jobb-ID</param>
        private async Task RemoveExistingShift(DateTime date, int jobProfileId)
        {
            var existingShift = await CheckForExistingShift(date, jobProfileId);
            if (existingShift != null)
            {
                _databaseService.WorkShifts.DeleteWorkShift(existingShift.Id);
            }
        }

        /// <summary>
        /// Hämtar läsbar text för ShiftType
        /// </summary>
        /// <param name="shiftType">ShiftType att konvertera</param>
        /// <returns>Läsbar text</returns>
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
