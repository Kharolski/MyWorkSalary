using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.Services.Handlers
{
    public class ShiftTypeHandler
    {
        #region Private Fields

        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly ISickLeaveRepository _sickLeaveRepository;
        private readonly VABHandler _vabHandler;
        private readonly SickLeaveHandler _sickLeaveHandler;

        #endregion

        #region Constructor

        public ShiftTypeHandler(
            IWorkShiftRepository workShiftRepository,
            ISickLeaveRepository sickLeaveRepository,
            VABHandler vabHandler,
            SickLeaveHandler sickLeaveHandler)
        {
            _workShiftRepository = workShiftRepository;
            _sickLeaveRepository = sickLeaveRepository;
            _vabHandler = vabHandler;
            _sickLeaveHandler = sickLeaveHandler;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Huvudkoordinator för olika typer av arbetspass
        /// </summary>
        public async Task<ShiftHandlerResult> HandleShiftType(
            ShiftType shiftType,
            DateTime date,
            JobProfile jobProfile,
            TimeSpan? startTime = null,
            TimeSpan? endTime = null)
        {
            // Kontrollera om det redan finns ett pass för detta datum
            var existingShift = CheckForExistingShift(jobProfile.Id, date);
            if (existingShift != null)
            {
                return await HandleExistingShift(existingShift, shiftType, date, jobProfile);
            }

            return shiftType switch
            {
                ShiftType.VAB => await _vabHandler.HandleVAB(date, jobProfile, startTime, endTime),
                ShiftType.SickLeave => HandleSickLeaveRequest(date, jobProfile),
                ShiftType.Regular => HandleRegular(date, startTime, endTime, jobProfile),
                _ => new ShiftHandlerResult { Success = false, Message = "Okänd passtyp" }
            };
        }

        #endregion

        #region Existing Shift Handling

        /// <summary>
        /// Kontrollerar om det finns ett befintligt pass för datumet
        /// </summary>
        private WorkShift? CheckForExistingShift(int jobProfileId, DateTime date)
        {
            var shifts = _workShiftRepository.GetWorkShiftsForDate(jobProfileId, date);  
            return shifts.FirstOrDefault();
        }

        /// <summary>
        /// Hanterar när det redan finns ett pass för datumet
        /// </summary>
        private async Task<ShiftHandlerResult> HandleExistingShift(
            WorkShift existingShift,
            ShiftType newShiftType,
            DateTime date,
            JobProfile jobProfile)
        {
            // Om samma typ - visa befintlig data
            if (existingShift.ShiftType == newShiftType)
            {
                return new ShiftHandlerResult
                {
                    Success = true,
                    CreatedShift = existingShift,
                    Message = string.Format(LocalizationHelper.Translate("Shift_ExistingSameType"), newShiftType, date.ToString("yyyy-MM-dd")),
                    ShowConfirmationDialog = true,
                    ConfirmationMessage = LocalizationHelper.Translate("Shift_EditExistingConfirmation")
                };
            }

            // Olika typ - fråga om ersättning
            return new ShiftHandlerResult
            {
                Success = false,
                Message = string.Format(LocalizationHelper.Translate("Shift_ExistingDifferentType"), existingShift.ShiftType, date.ToString("yyyy-MM-dd")),
                ShowConfirmationDialog = true,
                ConfirmationMessage = string.Format(LocalizationHelper.Translate("Shift_ReplaceExistingConfirmation"), existingShift.ShiftType, newShiftType),
                RequiresUserChoice = true
            };
        }

        #endregion

        #region Regular Shifts

        /// <summary>
        /// Hanterar vanliga arbetspass som kräver tid-input från användaren
        /// </summary>
        private ShiftHandlerResult HandleRegular(DateTime date, TimeSpan? startTime, TimeSpan? endTime, JobProfile jobProfile)
        {
            // Regular shifts hanteras av vanlig AddShift-logik
            return new ShiftHandlerResult
            {
                Success = true,
                RequiresTimeInput = true,
                Message = LocalizationHelper.Translate("Shift_EnterTime")
            };
        }

        #endregion

        #region VAB Handling

        /// <summary>
        /// Bekräftar ersättning för VAB
        /// </summary>
        public async Task<ShiftHandlerResult> ConfirmReplaceWithVAB(DateTime date, JobProfile jobProfile)
        {
            return await _vabHandler.ConfirmReplaceWithVAB(date, jobProfile);
        }

        #endregion

        #region Sick Leave Handling

        /// <summary>
        /// Hanterar sjukdag-förfrågan - kräver användarval av sjuktyp
        /// </summary>
        private ShiftHandlerResult HandleSickLeaveRequest(DateTime date, JobProfile jobProfile)
        {
            return new ShiftHandlerResult
            {
                Success = true,
                RequiresUserChoice = true,
                Message = LocalizationHelper.Translate("Shift_SelectSickType"),
                ConfirmationMessage = LocalizationHelper.Translate("Shift_ChooseSickType")
            };
        }

        /// <summary>
        /// Hanterar specifik sjukdag efter användarval
        /// </summary>
        public async Task<ShiftHandlerResult> HandleSickLeaveWithType(
            DateTime date,
            JobProfile jobProfile,
            SickLeaveType sickType,
            TimeSpan? workedStartTime = null,
            TimeSpan? workedEndTime = null,
            TimeSpan? scheduledStartTime = null,
            TimeSpan? scheduledEndTime = null)
        {
            try
            {
                // Använd befintlig metod som returnerar tuple
                var (workShift, sickLeave) = await _sickLeaveHandler.HandleSickLeave(
                    date,
                    jobProfile,
                    sickType,
                    workedStartTime,
                    workedEndTime,
                    scheduledStartTime,
                    scheduledEndTime);

                return new ShiftHandlerResult
                {
                    Success = true,
                    CreatedShift = workShift,
                    Message = LocalizationHelper.Translate("Shift_SickDayRegistered")
                };
            }
            catch (Exception ex)
            {
                return new ShiftHandlerResult
                {
                    Success = false,
                    Message = string.Format(LocalizationHelper.Translate("Shift_SickDayError"), ex.Message)
                };
            }
        }

        /// <summary>
        /// Hämtar sjukdata för ett befintligt pass
        /// </summary>
        public async Task<SickLeave?> GetSickLeaveForShift(int workShiftId)
        {
            return _sickLeaveRepository.GetSickLeaveByWorkShiftId(workShiftId);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Raderar ett pass och all relaterad data
        /// </summary>
        public bool DeleteShiftWithRelatedData(int workShiftId)  
        {
            try
            {
                // Hämta passet för att se vilken typ det är
                var workShift = _workShiftRepository.GetWorkShift(workShiftId);  
                if (workShift == null)
                    return false;

                // Radera relaterad specialiserad data först
                switch (workShift.ShiftType)
                {
                    case ShiftType.SickLeave:
                        var sickLeave = _sickLeaveRepository.GetSickLeaveByWorkShiftId(workShiftId);
                        if (sickLeave != null)
                        {
                            _sickLeaveRepository.DeleteSickLeave(sickLeave.Id);
                        }
                        break;

                        // case ShiftType.VAB:
                        //     // Hantera VAB-data
                        //     break;

                        // Lägg till fler typer här när de implementeras
                }

                // Radera huvudpasset
                _workShiftRepository.DeleteWorkShift(workShiftId);  
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fel vid radering av pass: {ex.Message}");
                return false;
            }
        }

        #endregion
    }

    #region Result Class

    public class ShiftHandlerResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool RequiresTimeInput { get; set; } = false;
        public WorkShift? CreatedShift { get; set; }
        public bool ShowConfirmationDialog { get; set; } = false;
        public string ConfirmationMessage { get; set; } = string.Empty;
        public bool RequiresUserChoice { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    #endregion
}
