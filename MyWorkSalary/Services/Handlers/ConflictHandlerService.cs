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
                    "🤒 Konflikt med arbetspass",
                    conflictMessage,
                    "Ja, ta bort passen",
                    "Nej, avbryt");

                if (removeShifts)
                {
                    var result = await _conflictResolutionService.SaveSickLeaveAndRemoveConflicts(sickShift);
                    if (result.Success)
                    {
                        await Shell.Current.DisplayAlert("✅ Sparat!", result.Message, "OK");
                        await Shell.Current.GoToAsync("..");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("⚠️ Fel", result.Message, "OK");
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
                    "🤒 Konflikt med sjukskrivning",
                    conflictMessage,
                    "Ja, förkorta sjukskrivningen",
                    "Nej, avbryt");

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
                                await Shell.Current.DisplayAlert("✅ Sparat!",
                                    $"{shortenResult.Message}\nArbetspasset har sparats!", "OK");
                                await Shell.Current.GoToAsync("..");
                            }
                            else
                            {
                                await Shell.Current.DisplayAlert("⚠️ Fel", saveResult.Message, "OK");
                            }
                        }
                        else
                        {
                            await Shell.Current.DisplayAlert("⚠️ Fel", shortenResult.Message, "OK");
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
                    ? "🤒 Konflikt med sjukskrivning"
                    : "🏖️ Konflikt med semester";

                bool removeWorkShift = await Shell.Current.DisplayAlert(
                    leaveType,
                    conflictMessage,
                    "Ja, ta bort arbetspasset",
                    "Nej, avbryt");

                if (removeWorkShift)
                {
                    var result = await _conflictResolutionService.RemoveWorkShiftAndSaveLeave(workShiftId, leaveShift);
                    if (result.Success)
                    {
                        await Shell.Current.DisplayAlert("✅ Sparat!", result.Message, "OK");
                        await Shell.Current.GoToAsync("..");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("⚠️ Fel", result.Message, "OK");
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
                    ? "🤒 Sammanslagning av sjukskrivning"
                    : "🏖️ Sammanslagning av semester";

                bool mergeShifts = await Shell.Current.DisplayAlert(
                    typeText,
                    mergeMessage,
                    "Ja, slå samman",
                    "Nej, avbryt");

                if (mergeShifts)
                {
                    var result = await _conflictResolutionService.MergeLeavePeriods(newShift, existingIds, mergedStartDate, mergedDays);
                    if (result.Success)
                    {
                        await Shell.Current.DisplayAlert("✅ Sammanslagen!", result.Message, "OK");
                        await Shell.Current.GoToAsync("..");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("⚠️ Fel", result.Message, "OK");
                    }
                }
            }
        }
        #endregion
    }
}
