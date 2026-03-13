using MyWorkSalary.Models.Core;
using MyWorkSalary.Services.Interfaces;
using System;

namespace MyWorkSalary.Helpers.Calendar
{
    public static class CalendarValidator
    {
        public static bool HasConflict(
            DateTime date,
            TimeSpan startTime,
            TimeSpan endTime,
            IWorkShiftRepository workShiftRepository,
            int jobProfileId)
        {
            try
            {
                // Hämta befintliga pass för datumet och nästa dag (för nattskift)
                var datesToCheck = new List<DateTime> { date };

                // Om sluttid är mindre än starttid → nattskift, kolla även nästa dag
                if (endTime < startTime)
                {
                    datesToCheck.Add(date.AddDays(1));
                }

                foreach (var checkDate in datesToCheck)
                {
                    var existingShifts = workShiftRepository.GetWorkShiftsForDate(jobProfileId, checkDate);

                    foreach (var shift in existingShifts)
                    {
                        if (shift.StartTime.HasValue && shift.EndTime.HasValue)
                        {
                            var existingStart = shift.StartTime.Value;
                            var existingEnd = shift.EndTime.Value;

                            // Skapa DateTime för nytt pass
                            var newStart = date.Date.Add(startTime);
                            var newEnd = endTime < startTime
                                ? date.Date.AddDays(1).Add(endTime) // Nattskift
                                : date.Date.Add(endTime); // Vanligt pass

                            // Kolla överlappning
                            if (TimeRangesOverlap(newStart, newEnd, existingStart, existingEnd))
                            {
                                return true; // Konflikt hittad
                            }
                        }
                    }
                }

                return false; // Ingen konflikt
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HasConflict: {ex.Message}");
                return false; // Antag ingen konflikt vid fel
            }
        }

        private static bool TimeRangesOverlap(
            DateTime start1, DateTime end1,
            DateTime start2, DateTime end2)
        {
            // Två tidsintervall överlappar om:
            // start1 < end2 && start2 < end1
            return start1 < end2 && start2 < end1;
        }
    }
}