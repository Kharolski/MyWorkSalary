using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.Services.Handlers
{
    public class ConflictHandlerService : IConflictHandlerService
    {
        #region Private Fields
        private readonly DatabaseService _databaseService;
        private readonly IConflictResolutionService _conflictResolutionService;
        private readonly IWorkShiftService _workShiftService;
        #endregion

        #region Constructor
        public ConflictHandlerService(
            DatabaseService databaseService,
            IConflictResolutionService conflictResolutionService,
            IWorkShiftService workShiftService)
        {
            _databaseService = databaseService;
            _conflictResolutionService = conflictResolutionService;
            _workShiftService = workShiftService;
        }
        #endregion

        #region Conflict Handlers
        public async Task HandleSickLeaveConflict(string message, WorkShift sickShift)
        {
            var parts = message.Split('|');
            if (parts.Length >= 2)
            {
                var conflictMessage = parts[1];
                bool removeShifts = await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("Conflict_Title_SickLeave_WorkShift"),
                    conflictMessage,
                    LocalizationHelper.Translate("Action_RemoveShifts"),
                    LocalizationHelper.Translate("Action_Cancel"));

                if (removeShifts)
                {
                    var result = await _conflictResolutionService.SaveSickLeaveAndRemoveConflicts(sickShift);
                    if (result.Success)
                    {
                        await Shell.Current.DisplayAlert(LocalizationHelper.Translate("Status_Saved"), result.Message, LocalizationHelper.Translate("Action_OK"));
                        await Shell.Current.GoToAsync("..");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert(
                            LocalizationHelper.Translate("Status_Error"),
                            result.Message,
                            LocalizationHelper.Translate("Action_OK"));
                    }
                }
            }
        }

        public async Task HandleWorkShiftSickConflict(string message, WorkShift workShift, JobProfile activeJob)
        {
            var parts = message.Split('|');
            if (parts.Length >= 4)
            {
                var conflictMessage = parts[1];
                var sickLeaveId = int.Parse(parts[2]);
                var workDate = DateTime.Parse(parts[3]);

                bool shortenSick = await Shell.Current.DisplayAlert(
                    LocalizationHelper.Translate("Conflict_Title_SickLeave"),
                    conflictMessage,
                    LocalizationHelper.Translate("Action_ShortenSickLeave"),
                    LocalizationHelper.Translate("Action_Cancel"));

                if (shortenSick)
                {
                    var sickLeave = _databaseService.WorkShifts.GetWorkShifts(activeJob.Id)
                        .FirstOrDefault(s => s.Id == sickLeaveId);

                    if (sickLeave != null)
                    {
                        // Förkorta till dagen innan arbetspasset
                        var newEndDate = workDate.AddDays(-1);
                        var shortenResult = await _conflictResolutionService.ShortenSickLeave(sickLeave, newEndDate);

                        if (shortenResult.Success)
                        {
                            // Nu spara arbetspasset
                            var saveResult = await _workShiftService.SaveWorkShiftWithValidation(workShift);
                            if (saveResult.Success)
                            {
                                await Shell.Current.DisplayAlert(
                                    LocalizationHelper.Translate("Status_Saved"),
                                    LocalizationHelper.Translate(
                                        "Conflict_SickLeave_Shortened_And_WorkShiftSaved",
                                        shortenResult.Message),
                                    LocalizationHelper.Translate("Action_OK"));
                                await Shell.Current.GoToAsync("..");
                            }
                            else
                            {
                                await Shell.Current.DisplayAlert(
                                    LocalizationHelper.Translate("Status_Error"),
                                    saveResult.Message,
                                    LocalizationHelper.Translate("Action_OK"));
                            }
                        }
                        else
                        {
                            await Shell.Current.DisplayAlert(
                                LocalizationHelper.Translate("Status_Error"),
                                shortenResult.Message,
                                LocalizationHelper.Translate("Action_OK"));
                        }
                    }
                }
            }
        }

        public async Task HandleWorkShiftConflict(string message, WorkShift leaveShift)
        {
            var parts = message.Split('|');
            if (parts.Length >= 4)
            {
                var conflictMessage = parts[1];
                var workShiftId = int.Parse(parts[2]);
                var workDate = DateTime.Parse(parts[3]);

                var leaveType = leaveShift.ShiftType == ShiftType.SickLeave
                    ? LocalizationHelper.Translate("Conflict_Title_SickLeave")
                    : LocalizationHelper.Translate("Conflict_Title_Vacation");

                bool removeWorkShift = await Shell.Current.DisplayAlert(
                    leaveType,
                    conflictMessage,
                    LocalizationHelper.Translate("Action_RemoveWorkShift"),
                    LocalizationHelper.Translate("Action_Cancel"));

                if (removeWorkShift)
                {
                    var result = await _conflictResolutionService.RemoveWorkShiftAndSaveLeave(workShiftId, leaveShift);
                    if (result.Success)
                    {
                        await Shell.Current.DisplayAlert(
                            LocalizationHelper.Translate("Status_Saved"),
                            result.Message,
                            LocalizationHelper.Translate("Action_OK"));
                        await Shell.Current.GoToAsync("..");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert(
                            LocalizationHelper.Translate("Status_Error"),
                            result.Message,
                            LocalizationHelper.Translate("Action_OK"));
                    }
                }
            }
        }

        public async Task HandlePeriodMerging(string message, WorkShift newShift)
        {
            var parts = message.Split('|');
            if (parts.Length >= 5)
            {
                var mergeMessage = parts[1];
                var existingIds = parts[2].Split(',').Select(int.Parse).ToList();
                var mergedStartDate = DateTime.Parse(parts[3]);
                var mergedDays = int.Parse(parts[4]);

                var typeText = newShift.ShiftType == ShiftType.SickLeave
                    ? LocalizationHelper.Translate("Merge_Title_SickLeave")
                    : LocalizationHelper.Translate("Merge_Title_Vacation");

                bool mergeShifts = await Shell.Current.DisplayAlert(
                    typeText,
                    mergeMessage,
                    LocalizationHelper.Translate("Action_Merge"),
                    LocalizationHelper.Translate("Action_Cancel"));

                if (mergeShifts)
                {
                    var result = await _conflictResolutionService.MergeLeavePeriods(newShift, existingIds, mergedStartDate, mergedDays);
                    if (result.Success)
                    {
                        await Shell.Current.DisplayAlert(
                            LocalizationHelper.Translate("Status_Merged"),
                            result.Message,
                            LocalizationHelper.Translate("Action_OK"));
                        await Shell.Current.GoToAsync("..");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert(LocalizationHelper.Translate("Status_Error"), result.Message, LocalizationHelper.Translate("Action_OK"));
                    }
                }
            }
        }
        #endregion
    }
}
