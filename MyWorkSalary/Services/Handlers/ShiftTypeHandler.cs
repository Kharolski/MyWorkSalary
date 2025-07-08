using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.Services.Handlers
{
    public class ShiftTypeHandler
    {
        #region Private Fields
        private readonly VABHandler _vabHandler;
        private readonly SickLeaveHandler _sickLeaveHandler;
        //private readonly VacationHandler _vacationHandler;
        //private readonly OnCallHandler _onCallHandler;
        #endregion

        #region Constructor
        public ShiftTypeHandler(VABHandler vabHandler, 
            SickLeaveHandler sickLeaveHandler)
            //VacationHandler vacationHandler,
            //OnCallHandler onCallHandler)
        {
            _vabHandler = vabHandler;
            _sickLeaveHandler = sickLeaveHandler;
            //_vacationHandler = vacationHandler;
            //_onCallHandler = onCallHandler;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Huvudkoordinator för olika typer av arbetspass
        /// </summary>
        /// <param name="shiftType">Typ av pass att hantera</param>
        /// <param name="date">Datum för passet</param>
        /// <param name="jobProfile">Jobbprofil för beräkningar</param>
        /// <param name="startTime">Starttid (för Regular/OnCall)</param>
        /// <param name="endTime">Sluttid (för Regular/OnCall)</param>
        /// <returns>Resultat av passhantering</returns>
        public async Task<ShiftHandlerResult> HandleShiftType(ShiftType shiftType, DateTime date, JobProfile jobProfile, TimeSpan? startTime = null, TimeSpan? endTime = null)
        {
            return shiftType switch
            {
                ShiftType.VAB => await _vabHandler.HandleVAB(date, jobProfile, startTime, endTime),
                ShiftType.SickLeave => HandleSickLeaveRequest(date, jobProfile),
                //ShiftType.Vacation => await _vacationHandler.HandleVacation(date, jobProfile),
                //ShiftType.OnCall => await _onCallHandler.HandleOnCall(date, startTime, endTime, jobProfile),
                ShiftType.Regular => HandleRegular(date, startTime, endTime, jobProfile),
                _ => new ShiftHandlerResult { Success = false, Message = "Okänd passtyp" }
            };
        }
        #endregion

        #region Vanligt arbetspass
        /// <summary>
        /// Hanterar vanliga arbetspass som kräver tid-input från användaren
        /// </summary>
        /// <param name="date">Datum för arbetspasset</param>
        /// <param name="startTime">Starttid (kan vara null om inte angiven än)</param>
        /// <param name="endTime">Sluttid (kan vara null om inte angiven än)</param>
        /// <param name="jobProfile">Jobbprofil för beräkningar</param>
        /// <returns>Resultat som indikerar att tid-input krävs</returns>
        private ShiftHandlerResult HandleRegular(DateTime date, TimeSpan? startTime, TimeSpan? endTime, JobProfile jobProfile)
        {
            // Regular shifts hanteras av vanlig AddShift-logik
            return new ShiftHandlerResult
            {
                Success = true,
                RequiresTimeInput = true,
                Message = "Ange start- och sluttid för arbetspasset"
            };
        }
        #endregion

        #region VAB
        /// <summary>
        /// Bekräftar ersättning för VAB
        /// </summary>
        public async Task<ShiftHandlerResult> ConfirmReplaceWithVAB(DateTime date, JobProfile jobProfile)
        {
            return await _vabHandler.ConfirmReplaceWithVAB(date, jobProfile);
        }
        #endregion

        #region Sjukskrivning
        /// <summary>
        /// Hanterar sjukdag-förfrågan - kräver användarval av sjuktyp
        /// </summary>
        /// <param name="date">Datum för sjukdagen</param>
        /// <param name="jobProfile">Jobbprofil</param>
        /// <returns>Resultat som kräver användarval</returns>
        private ShiftHandlerResult HandleSickLeaveRequest(DateTime date, JobProfile jobProfile)
        {
            return new ShiftHandlerResult
            {
                Success = true,
                RequiresUserChoice = true,
                Message = "Välj typ av sjukdag",
                ConfirmationMessage = "Vilken typ av sjukdag vill du registrera?"
            };
        }

        /// <summary>
        /// Hanterar specifik sjukdag efter användarval
        /// </summary>
        /// <param name="date">Datum för sjukdagen</param>
        /// <param name="jobProfile">Jobbprofil</param>
        /// <param name="sickType">Typ av sjukdag</param>
        /// <param name="workedHours">Timmar som jobbades (om delvis sjuk)</param>
        /// <param name="scheduledHours">Timmar som skulle jobbats</param>
        /// <returns>Resultat med skapad WorkShift</returns>
        public async Task<ShiftHandlerResult> HandleSickLeaveWithType(
            DateTime date,
            JobProfile jobProfile,
            SickLeaveType sickType,
            TimeSpan? workedHours = null,
            TimeSpan? scheduledHours = null)
        {
            try
            {
                var workShift = await _sickLeaveHandler.HandleSickLeave(
                    date, jobProfile, sickType, workedHours, scheduledHours);

                return new ShiftHandlerResult
                {
                    Success = true,
                    CreatedShift = workShift,
                    Message = "Sjukdag registrerad"
                };
            }
            catch (Exception ex)
            {
                return new ShiftHandlerResult
                {
                    Success = false,
                    Message = $"Fel vid registrering av sjukdag: {ex.Message}"
                };
            }
        }
        #endregion
    }

    #region Result Class
    public class ShiftHandlerResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool RequiresTimeInput { get; set; } = false;
        public WorkShift CreatedShift { get; set; }
        public bool ShowConfirmationDialog { get; set; } = false;
        public string ConfirmationMessage { get; set; }
        public bool RequiresUserChoice { get; set; } = false;
    }
    #endregion
}
