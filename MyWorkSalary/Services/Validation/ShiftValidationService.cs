using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;
using System.Globalization;

namespace MyWorkSalary.Services.Validation
{
    public class ShiftValidationService : IShiftValidationService
    {
        private readonly DatabaseService _databaseService;

        public ShiftValidationService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        // Validera att sjukperiod är rimlig
        //public (bool IsValid, string Message) ValidateSickLeave(WorkShift sickShift)
        //{
        //    if (sickShift.ShiftType != ShiftType.SickLeave)
        //        return (true, "");

        //    var days = sickShift.NumberOfDays ?? 1;

        //    // Kontrollera rimliga värden
        //    if (days <= 0)
        //        return (false, "Antal dagar måste vara större än 0");

        //    if (days > 365)
        //        return (false, "Sjukperiod kan inte vara längre än 365 dagar");

        //    // Varna för långa perioder
        //    if (days > 14)
        //    {
        //        return (true, $"⚠️ Lång sjukperiod ({days} dagar). Efter dag 7 betalar Försäkringskassan.");
        //    }

        //    return (true, "");
        //}

        /// <summary>
        /// Kontrollera om semester kan läggas på valt datum
        /// </summary>
        public (bool CanAdd, string ErrorMessage, List<WorkShift> ConflictingShifts) ValidateVacationDate(
            int jobProfileId,
            DateTime vacationDate,
            VacationType vacationType)
        {
            var conflictingShifts = new List<WorkShift>();
            var existingShifts = _databaseService.WorkShifts.GetWorkShifts(jobProfileId)
                .Where(x => x.ShiftDate.Date == vacationDate.Date)
                .ToList();

            if (!existingShifts.Any())
                return (true, "", conflictingShifts);

            foreach (var existing in existingShifts)
            {
                switch (existing.ShiftType)
                {
                    case ShiftType.Vacation:
                        conflictingShifts.Add(existing);
                        return (false,
                            $"Det finns redan semester registrerat denna dag.\n\n" +
                            $"📅 {vacationDate:dddd d MMMM}\n" +
                            $"🏖️ Befintlig semester\n\n" +
                            $"Ta bort den befintliga semestern först.",
                            conflictingShifts);

                    case ShiftType.SickLeave:
                        conflictingShifts.Add(existing);
                        return (false,
                            $"Du är sjukskriven denna dag.\n\n" +
                            $"📅 {vacationDate:dddd d MMMM}\n" +
                            $"🤒 Sjukskrivning ({existing.NumberOfDays ?? 1} dagar)\n\n" +
                            $"Ta bort sjukskrivningen först.",
                            conflictingShifts);

                    case ShiftType.Regular:
                    case ShiftType.OnCall:
                        conflictingShifts.Add(existing);
                        var shiftInfo = existing.StartTime.HasValue
                            ? $"{existing.StartTime:HH:mm} - {existing.EndTime:HH:mm}"
                            : "Arbetspass";
                        return (false,
                            $"Det finns redan ett arbetspass registrerat denna dag.\n\n" +
                            $"📅 {vacationDate:dddd d MMMM}\n" +
                            $"💼 {shiftInfo}\n\n" +
                            $"Ta bort arbetspasset först.",
                            conflictingShifts);

                    case ShiftType.VAB:
                        conflictingShifts.Add(existing);
                        return (false,
                            $"Du har VAB registrerat denna dag.\n\n" +
                            $"📅 {vacationDate:dddd d MMMM}\n" +
                            $"👶 Vård av barn\n\n" +
                            $"Ta bort VAB:en först.",
                            conflictingShifts);
                }
            }

            return (true, "", conflictingShifts);
        }

        // Hantera Semester/Sjuk korrekt
        //public bool HasOverlappingShift(WorkShift newShift)
        //{
        //    // SKIPPA kontrol för semester/sjuk - de har inga tider
        //    if (newShift.ShiftType == ShiftType.Vacation ||
        //        newShift.ShiftType == ShiftType.SickLeave ||
        //        !newShift.StartTime.HasValue ||
        //        !newShift.EndTime.HasValue)
        //    {
        //        return false;
        //    }

        //    var existingShifts = _databaseService.WorkShifts.GetWorkShifts(newShift.JobProfileId);

        //    foreach (var existing in existingShifts)
        //    {
        //        // Skippa om vi uppdaterar samma pass
        //        if (existing.Id == newShift.Id)
        //            continue;

        //        // SKIPPA befintliga pass utan tider
        //        if (!existing.StartTime.HasValue || !existing.EndTime.HasValue)
        //            continue;

        //        // Kontrollera överlapp mellan tidsperioder
        //        if (newShift.StartTime < existing.EndTime &&
        //            newShift.EndTime > existing.StartTime)
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        // Returnera överlappande pass med säker null-hantering
        public WorkShift? GetOverlappingShift(WorkShift newShift)
        {
            // SKIPPA kontrol för semester/sjuk
            if (newShift.ShiftType == ShiftType.Vacation ||
                newShift.ShiftType == ShiftType.SickLeave ||
                !newShift.StartTime.HasValue ||
                !newShift.EndTime.HasValue)
            {
                return null;
            }

            var existingShifts = _databaseService.WorkShifts.GetWorkShifts(newShift.JobProfileId);

            foreach (var existing in existingShifts)
            {
                // Skippa om vi uppdaterar samma pass
                if (existing.Id == newShift.Id)
                    continue;

                // SKIPPA befintliga pass utan tider
                if (!existing.StartTime.HasValue || !existing.EndTime.HasValue)
                    continue;

                // Kontrollera överlapp mellan tidsperioder
                if (newShift.StartTime < existing.EndTime &&
                    newShift.EndTime > existing.StartTime)
                {
                    return existing;
                }
            }

            return null;
        }

        // Kontrollera om samma typ av ledighet redan finns på datum
        //public bool HasConflictingLeave(WorkShift newShift)
        //{
        //    // Endast för Semester/Sjuk
        //    if (newShift.ShiftType != ShiftType.Vacation &&
        //        newShift.ShiftType != ShiftType.SickLeave)
        //    {
        //        return false;
        //    }

        //    var existingShifts = _databaseService.WorkShifts.GetWorkShifts(newShift.JobProfileId);

        //    foreach (var existing in existingShifts)
        //    {
        //        // Skippa om vi uppdaterar samma pass
        //        if (existing.Id == newShift.Id)
        //            continue;

        //        // Kontrollera överlappande perioder
        //        if (existing.ShiftType == ShiftType.Vacation ||
        //            existing.ShiftType == ShiftType.SickLeave)
        //        {
        //            // Beräkna start- och slutdatum för båda passen
        //            var newStart = newShift.ShiftDate.Date;
        //            var newEnd = newStart.AddDays((newShift.NumberOfDays ?? 1) - 1);

        //            var existingStart = existing.ShiftDate.Date;
        //            var existingEnd = existingStart.AddDays((existing.NumberOfDays ?? 1) - 1);

        //            // Kontrollera om perioderna överlappar
        //            if (newStart <= existingEnd && newEnd >= existingStart)
        //            {
        //                return true;
        //            }
        //        }

        //        // Kontrollera mot vanliga pass också
        //        if (existing.StartTime.HasValue && newShift.ShiftType == ShiftType.SickLeave)
        //        {
        //            var existingDate = existing.StartTime.Value.Date;
        //            var newStart = newShift.ShiftDate.Date;
        //            var newEnd = newStart.AddDays((newShift.NumberOfDays ?? 1) - 1);

        //            // Kan inte jobba när man är sjuk
        //            if (existingDate >= newStart && existingDate <= newEnd)
        //            {
        //                return true;
        //            }
        //        }
        //    }

        //    return false;
        //}

        // Kontrollera om datum redan har pass
        //public bool HasShiftOnDate(int jobProfileId, DateTime date, ShiftType? shiftType = null)
        //{
        //    var query = _databaseService.WorkShifts.GetWorkShifts(jobProfileId)
        //                               .Where(x => x.ShiftDate.Date == date.Date);

        //    if (shiftType.HasValue)
        //    {
        //        query = query.Where(x => x.ShiftType == shiftType.Value);
        //    }

        //    return query.Any();
        //}

        // Kontrollera arbetspass mot hela dagen sjuk/semester
        public (bool HasConflict, string ConflictMessage, WorkShift ConflictingLeave) CheckWorkShiftAgainstFullDayLeave(WorkShift workShift)
        {
            // Skippa om det inte är ett arbetspass med tid
            if (workShift.ShiftType == ShiftType.SickLeave ||
                workShift.ShiftType == ShiftType.Vacation ||
                !workShift.StartTime.HasValue)
                return (false, "", null);

            var workDate = workShift.StartTime.Value.Date;
            var existingShifts = _databaseService.WorkShifts.GetWorkShifts(workShift.JobProfileId);

            foreach (var existing in existingShifts)
            {
                if (existing.Id == workShift.Id)
                    continue;

                // Kontrollera mot sjukskrivning (hela dagen)
                if (existing.ShiftType == ShiftType.SickLeave)
                {
                    var sickStart = existing.ShiftDate.Date;
                    var sickEnd = sickStart.AddDays((existing.NumberOfDays ?? 1) - 1);

                    // Om arbetspasset är NÅGON dag under sjukperioden = konflikt
                    if (workDate >= sickStart && workDate <= sickEnd)
                    {
                        var swedishCulture = new CultureInfo("sv-SE");
                        var message = $"Du är sjukskriven denna dag (HELA DAGEN):\n\n" +
                                     $"📅 Sjukperiod: {sickStart.ToString("d MMM", swedishCulture)} - {sickEnd.ToString("d MMM", swedishCulture)}\n" +
                                     $"({existing.NumberOfDays} dagar)\n\n" +
                                     $"Du kan inte jobba när du är sjukskriven.\n" +
                                     $"Vill du förkorta sjukskrivningen?";

                        return (true, message, existing);
                    }
                }

                // Kontrollera mot semester (hela dagen)  
                if (existing.ShiftType == ShiftType.Vacation)
                {
                    var vacationStart = existing.ShiftDate.Date;
                    var vacationEnd = vacationStart.AddDays((existing.NumberOfDays ?? 1) - 1);

                    // Om arbetspasset är NÅGON dag under semesterperioden = konflikt
                    if (workDate >= vacationStart && workDate <= vacationEnd)
                    {
                        var swedishCulture = new CultureInfo("sv-SE");
                        var message = $"Du har semester denna dag (HELA DAGEN):\n\n" +
                                     $"📅 Semesterperiod: {vacationStart.ToString("d MMM", swedishCulture)} - {vacationEnd.ToString("d MMM", swedishCulture)}\n" +
                                     $"({existing.NumberOfDays} dagar)\n\n" +
                                     $"Du kan inte jobba när du har semester.\n" +
                                     $"Vill du förkorta semestern?";

                        return (true, message, existing);
                    }
                }
            }

            return (false, "", null);
        }
    }
}
