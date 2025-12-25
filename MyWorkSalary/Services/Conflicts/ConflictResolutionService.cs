using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;
using System.Globalization;

namespace MyWorkSalary.Services.Conflicts
{
    public class ConflictResolutionService : IConflictResolutionService
    {
        private readonly DatabaseService _databaseService;

        public ConflictResolutionService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        #region Konflikthantering - Detaljerad konfliktanalys
        // Få detaljerad konfliktinfo
        public (bool HasConflict, string ConflictMessage, List<WorkShift> ConflictingShifts) GetLeaveConflictDetails(WorkShift newShift)
        {
            if (newShift.ShiftType != ShiftType.Vacation && newShift.ShiftType != ShiftType.SickLeave)
            {
                return (false, "", new List<WorkShift>());
            }

            var existingShifts = _databaseService.WorkShifts.GetWorkShifts(newShift.JobProfileId);
            var swedishCulture = new System.Globalization.CultureInfo("sv-SE");
            var conflictingShifts = new List<WorkShift>();

            var newStart = newShift.ShiftDate.Date;
            var newEnd = newStart.AddDays((newShift.NumberOfDays ?? 1) - 1);
            var leaveType = newShift.ShiftType == ShiftType.SickLeave
                ? LocalizationHelper.Translate("Shift_SickLeave")
                : LocalizationHelper.Translate("Shift_Vacation");

            foreach (var existing in existingShifts)
            {
                if (existing.Id == newShift.Id)
                    continue;

                // Kontrollera mot arbetspass (högsta prioritet)
                if (existing.StartTime.HasValue && existing.EndTime.HasValue)
                {
                    var workDate = existing.StartTime.Value.Date;

                    // Kontrollera om arbetspasset överlappar med ledighetsperioden
                    if (workDate >= newStart && workDate <= newEnd)
                    {
                        var startTime = existing.StartTime.Value.ToString("HH:mm");
                        var endTime = existing.EndTime.Value.ToString("HH:mm");
                        var dayName = workDate.ToString("dddd d MMMM", swedishCulture);

                        return (true,
                            $"WORK_CONFLICT|" +
                            $"{LocalizationHelper.Translate("Conflict_WorkDuringLeave_Title", leaveType)}\n\n" +
                            $"📅 {dayName}\n" +
                            $"🕐 {startTime} - {endTime}\n\n" +
                            $"{LocalizationHelper.Translate("Conflict_WorkDuringLeave_Body", leaveType)}\n" +
                            $"{LocalizationHelper.Translate("Conflict_WorkDuringLeave_ConfirmRemove")}" +
                            $"|{existing.Id}|{workDate:yyyy-MM-dd}",
                            new List<WorkShift> { existing });

                        //return (true,
                        //    $"WORK_CONFLICT|Du har ett arbetspass under {leaveType}en:\n\n" +
                        //    $"📅 {dayName}\n" +
                        //    $"🕐 {startTime} - {endTime}\n\n" +
                        //    $"Du kan inte ha {leaveType} samma dag som du arbetar.\n" +
                        //    $"Vill du ta bort arbetspasset?|{existing.Id}|{workDate:yyyy-MM-dd}",
                        //    new List<WorkShift> { existing });
                    }
                }

                // Kontrollera samma typ av ledighet (sammanslagning)
                if (existing.ShiftType == newShift.ShiftType && !existing.StartTime.HasValue)
                {
                    var existingStart = existing.ShiftDate.Date;
                    var existingEnd = existingStart.AddDays((existing.NumberOfDays ?? 1) - 1);

                    // Kontrollera överlapp ELLER sammanhängande dagar
                    if (PeriodsOverlapOrAdjacent(newStart, newEnd, existingStart, existingEnd))
                    {
                        conflictingShifts.Add(existing);
                    }
                }
                // Kontrollera mot andra typ av ledighet
                else if ((existing.ShiftType == ShiftType.Vacation || existing.ShiftType == ShiftType.SickLeave) &&
                         !existing.StartTime.HasValue)
                {
                    var existingStart = existing.ShiftDate.Date;
                    var existingEnd = existingStart.AddDays((existing.NumberOfDays ?? 1) - 1);

                    if (newStart <= existingEnd && newEnd >= existingStart)
                    {
                        var existingType = existing.ShiftType == ShiftType.Vacation
                            ? LocalizationHelper.Translate("Shift_Vacation")
                            : LocalizationHelper.Translate("Shift_SickLeave");
                        return (true,
                            $"{LocalizationHelper.Translate("Conflict_OverlappingLeave", existingType)}\n" +
                            $"📅 {existingStart.ToString("d MMM", swedishCulture)} - {existingEnd.ToString("d MMM", swedishCulture)}\n" +
                            $"{LocalizationHelper.Translate("Conflict_Days", existing.NumberOfDays)}",
                            new List<WorkShift>());

                        //return (true, $"Överlappar med befintlig {existingType.ToLower()}:\n" +
                        //             $"📅 {existingStart.ToString("d MMM", swedishCulture)} - {existingEnd.ToString("d MMM", swedishCulture)}\n" +
                        //             $"({existing.NumberOfDays} dagar)", new List<WorkShift>());
                    }
                }
            }

            // Om vi har sammanhängande perioder - föreslå sammanslagning
            if (conflictingShifts.Any())
            {
                var typeText = newShift.ShiftType == ShiftType.SickLeave
                    ? LocalizationHelper.Translate("Shift_SickLeave")
                    : LocalizationHelper.Translate("Shift_Vacation");
                var totalDays = conflictingShifts.Sum(s => s.NumberOfDays ?? 1) + (newShift.NumberOfDays ?? 1);
                var allDates = conflictingShifts.Select(s => s.ShiftDate.Date)
                    .Concat(new[] { newStart })
                    .OrderBy(d => d).ToList();

                var earliestDate = allDates.First();
                var latestDate = allDates.Last();

                // Beräkna verklig slutdag baserat på längsta perioden
                foreach (var shift in conflictingShifts)
                {
                    var endDate = shift.ShiftDate.AddDays((shift.NumberOfDays ?? 1) - 1);
                    if (endDate > latestDate)
                        latestDate = endDate;
                }

                var newEndDate = newStart.AddDays((newShift.NumberOfDays ?? 1) - 1);
                if (newEndDate > latestDate)
                    latestDate = newEndDate;

                var message = LocalizationHelper.Translate("Conflict_Merge_Header", typeText) + "\n\n";
                foreach (var conflict in conflictingShifts.OrderBy(s => s.ShiftDate))
                {
                    var start = conflict.ShiftDate;
                    var end = start.AddDays((conflict.NumberOfDays ?? 1) - 1);
                    // message += $"• {start.ToString("d MMM", swedishCulture)} - {end.ToString("d MMM", swedishCulture)} ({conflict.NumberOfDays} dagar)\n";
                    message += $"• {start: d MMM} - {end: d MMM} " + LocalizationHelper.Translate("Conflict_Days", conflict.NumberOfDays) + "\n";
                }

                var totalRealDays = (int)(latestDate - earliestDate).TotalDays + 1;
                message += "\n" + LocalizationHelper.Translate("Conflict_Merge_Question") + "\n";
                // message += $"📅 {earliestDate.ToString("d MMM", swedishCulture)} - {latestDate.ToString("d MMM", swedishCulture)} ({totalRealDays} dagar)";
                message += $"📅 {earliestDate:d MMM} - {latestDate:d MMM} " + LocalizationHelper.Translate("Conflict_Days", totalRealDays);

                return (true, $"MERGE_PERIODS|{message}|{string.Join(",", conflictingShifts.Select(s => s.Id))}|{earliestDate:yyyy-MM-dd}|{totalRealDays}", conflictingShifts);
            }

            return (false, "", new List<WorkShift>());
        }

        // Kontrollera om arbetspass kolliderar med sjukskrivning
        public (bool HasConflict, string ConflictMessage, WorkShift ConflictingSickLeave) CheckWorkShiftAgainstSickLeave(WorkShift workShift)
        {
            if (workShift.ShiftType == ShiftType.SickLeave || !workShift.StartTime.HasValue)
                return (false, "", null);

            var existingShifts = _databaseService.WorkShifts.GetWorkShifts(workShift.JobProfileId);
            var workDate = workShift.StartTime.Value.Date;

            foreach (var existing in existingShifts)
            {
                if (existing.Id == workShift.Id || existing.ShiftType != ShiftType.SickLeave)
                    continue;

                var sickStart = existing.ShiftDate.Date;
                var sickEnd = sickStart.AddDays((existing.NumberOfDays ?? 1) - 1);

                if (workDate >= sickStart && workDate <= sickEnd)
                {
                    var swedishCulture = new System.Globalization.CultureInfo("sv-SE");
                    //var message = $"Du är sjukskriven denna dag:\n\n" +
                    //             $"📅 Sjukperiod: {sickStart.ToString("d MMM", swedishCulture)} - {sickEnd.ToString("d MMM", swedishCulture)}\n" +
                    //             $"({existing.NumberOfDays} dagar)\n\n" +
                    //             $"Vill du förkorta sjukskrivningen för att kunna jobba?";
                    var message =
                            LocalizationHelper.Translate("Conflict_SickDay") + "\n\n" +
                            $"📅 {LocalizationHelper.Translate("Conflict_SickPeriod", sickStart.ToString("d MMM", swedishCulture), sickEnd.ToString("d MMM", swedishCulture))}\n" +
                            LocalizationHelper.Translate("Conflict_Days", existing.NumberOfDays) + "\n\n" +
                            LocalizationHelper.Translate("Conflict_ShortenSickLeave_Question");

                    return (true, message, existing);
                }
            }

            return (false, "", null);
        }

        // Hantera sjukskrivning med automatisk borttagning av arbetspass
        public async Task<(bool Success, string Message)> SaveSickLeaveWithConflictResolution(WorkShift sickShift)
        {
            try
            {
                // Hitta arbetspass under sjukperioden
                var conflictingShifts = GetWorkShiftsDuringSickLeave(sickShift);
                if (conflictingShifts.Any())
                {
                    // Fråga användare om automatisk borttagning
                    var swedishCulture = new System.Globalization.CultureInfo("sv-SE");
                    var conflictList = string.Join("\n", conflictingShifts.Select(s =>
                        $"• {s.StartTime?.ToString("dddd d MMM", swedishCulture)} ({s.StartTime:HH:mm}-{s.EndTime:HH:mm})"));

                    //var message = $"Du har {conflictingShifts.Count} arbetspass under sjukperioden:\n\n" +
                    //             $"{conflictList}\n\n" +
                    //             $"Vill du att systemet automatiskt tar bort dessa pass?";
                    var message =
                            LocalizationHelper.Translate("Conflict_AutoRemove_Header", conflictingShifts.Count) + "\n\n" +
                            $"{conflictList}\n\n" +
                            LocalizationHelper.Translate("Conflict_AutoRemove_Question");

                    // Returnera för att låta UI hantera bekräftelsen
                    return (false, $"CONFLICT_RESOLUTION_NEEDED|{message}|{sickShift.Id}");
                }

                // Kontrollera överlapp med andra ledigheter
                var leaveConflict = GetLeaveConflictDetails(sickShift);
                if (leaveConflict.HasConflict)
                {
                    return (false, leaveConflict.ConflictMessage);
                }

                // Spara sjukskrivningen
                _databaseService.WorkShifts.SaveWorkShift(sickShift);
                // return (true, $"Sjukskrivning på {sickShift.NumberOfDays} dagar har sparats!");
                return (true, LocalizationHelper.Translate("Shift_SickSaved", sickShift.NumberOfDays));
            }
            catch (Exception ex)
            {
                return (false, $"Fel vid sparande: {ex.Message}");
            }
        }

        #endregion

        #region Konfliktlösning - Automatiska åtgärder
        // Spara sjukskrivning och ta bort konflikterande pass
        public async Task<(bool Success, string Message)> SaveSickLeaveAndRemoveConflicts(WorkShift sickShift)
        {
            try
            {
                // Hitta och ta bort konflikterande pass
                var conflictingShifts = GetWorkShiftsDuringSickLeave(sickShift);
                foreach (var conflictShift in conflictingShifts)
                {
                    _databaseService.WorkShifts.DeleteWorkShift(conflictShift.Id);
                }

                // Spara sjukskrivningen
                _databaseService.WorkShifts.SaveWorkShift(sickShift);

                //var message = conflictingShifts.Any()
                //    ? $"Sjukskrivning sparad! {conflictingShifts.Count} arbetspass har tagits bort."
                //    : $"Sjukskrivning på {sickShift.NumberOfDays} dagar har sparats!";
                var message = conflictingShifts.Any()
                    ? LocalizationHelper.Translate("Shift_SickSavedWithRemoved", conflictingShifts.Count)
                    : LocalizationHelper.Translate("Shift_SickSaved", sickShift.NumberOfDays);

                return (true, message);
            }
            catch (Exception ex)
            {
                return (false, $"Fel vid sparande: {ex.Message}");
            }
        }

        // Förkorta sjukskrivning
        public async Task<(bool Success, string Message)> ShortenSickLeave(WorkShift sickLeave, DateTime newEndDate)
        {
            try
            {
                var sickStart = sickLeave.ShiftDate.Date;
                var newDays = (int)(newEndDate - sickStart).TotalDays + 1;

                if (newDays <= 0)
                {
                    // Ta bort hela sjukskrivningen
                    _databaseService.WorkShifts.DeleteWorkShift(sickLeave.Id);
                    return (true, LocalizationHelper.Translate("Shift_SickRemoved"));
                }
                else
                {
                    // Förkorta sjukskrivningen
                    sickLeave.NumberOfDays = newDays;
                    _databaseService.WorkShifts.SaveWorkShift(sickLeave);
                    var swedishCulture = new System.Globalization.CultureInfo("sv-SE");

                    // return (true, $"Sjukskrivningen förkortad till {newDays} dagar (till {newEndDate.ToString("d MMM", swedishCulture)}).");
                    return (true,
                        LocalizationHelper.Translate(
                            "Shift_SickShortened",
                            newDays,
                            newEndDate.ToString("d MMM", swedishCulture)));
                }
            }
            catch (Exception ex)
            {
                return (false, $"Fel vid ändring av sjukskrivning: {ex.Message}");
            }
        }

        // Slå samman perioder
        public async Task<(bool Success, string Message)> MergeLeavePeriods(WorkShift newShift, List<int> existingShiftIds, DateTime mergedStartDate, int mergedDays)
        {
            try
            {
                // Ta bort befintliga perioder
                foreach (var id in existingShiftIds)
                {
                    _databaseService.WorkShifts.DeleteWorkShift(id);
                }

                // Skapa ny sammanslagen period
                var mergedShift = new WorkShift
                {
                    JobProfileId = newShift.JobProfileId,
                    ShiftDate = mergedStartDate,
                    StartTime = null,
                    EndTime = null,
                    ShiftType = newShift.ShiftType,
                    NumberOfDays = mergedDays,
                    TotalHours = 0,
                    RegularHours = 0,
                    OBHours = 0,
                    RegularPay = 0, // Beräknas om vid behov
                    OBPay = 0,
                    TotalPay = 0,
                    Notes = newShift.Notes,
                    CreatedDate = DateTime.Now,
                    IsConfirmed = false,
                    //IsKarensDay = newShift.ShiftType == ShiftType.SickLeave,
                    //SickPayPercentage = newShift.ShiftType == ShiftType.SickLeave ? 0.8m : null
                };

                _databaseService.WorkShifts.SaveWorkShift(mergedShift);

                var typeText = newShift.ShiftType == ShiftType.SickLeave
                        ? LocalizationHelper.Translate("Shift_SickLeave")
                        : LocalizationHelper.Translate("Shift_Vacation");

                // return (true, $"{typeText} sammanslagen till {mergedDays} dagar!");
                return (true, LocalizationHelper.Translate("Shift_Merged", typeText, mergedDays));
            }
            catch (Exception ex)
            {
                return (false, $"Fel vid sammanslagning: {ex.Message}");
            }
        }

        #endregion

        #region Hjälpmetod
        public List<WorkShift> GetWorkShiftsDuringSickLeave(WorkShift sickShift)
        {
            var sickStart = sickShift.ShiftDate.Date;
            var sickEnd = sickStart.AddDays((sickShift.NumberOfDays ?? 1) - 1);

            return _databaseService.WorkShifts.GetWorkShifts(sickShift.JobProfileId)
                .Where(s => s.Id != sickShift.Id &&
                           s.StartTime.HasValue &&
                           s.StartTime.Value.Date >= sickStart &&
                           s.StartTime.Value.Date <= sickEnd)
                .ToList();
        }

        private bool PeriodsOverlapOrAdjacent(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {
            // Kontrollera överlapp ELLER om de är sammanhängande (en dag mellan)
            return start1 <= end2.AddDays(1) && end1 >= start2.AddDays(-1);
        }
        #endregion

        // Ta bort arbetspass och spara ledighet
        public async Task<(bool Success, string Message)> RemoveWorkShiftAndSaveLeave(int workShiftId, WorkShift leaveShift)
        {
            try
            {
                // Ta bort arbetspasset
                _databaseService.WorkShifts.DeleteWorkShift(workShiftId);

                // Spara ledigheten
                _databaseService.WorkShifts.SaveWorkShift(leaveShift);

                var typeText = leaveShift.ShiftType == ShiftType.SickLeave
                        ? LocalizationHelper.Translate("Shift_SickLeave")
                        : LocalizationHelper.Translate("Shift_Vacation");

                return (true, LocalizationHelper.Translate("Shift_WorkRemovedAndLeaveSaved", typeText));
            }
            catch (Exception ex)
            {
                return (false, $"Fel vid sparande: {ex.Message}");
            }
        }

    }
}

