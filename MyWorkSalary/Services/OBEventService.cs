using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.Services
{
    public class OBEventService : IOBEventService
    {
        private readonly IOBEventRepository _obEventRepository;
        private readonly IOBRateRepository _obRateRepository;
        private readonly IWorkShiftRepository _workShiftRepository;

        public OBEventService(IOBEventRepository obEventRepository, IOBRateRepository obRateRepository, IWorkShiftRepository workShiftRepository)
        {
            _obEventRepository = obEventRepository;
            _obRateRepository = obRateRepository;
            _workShiftRepository = workShiftRepository;
        }

        // Overload 1: hämta rates från DB
        public ObSummary RebuildForWorkShift(WorkShift shift)
        {
            var rates = _obRateRepository.GetOBRates(shift.JobProfileId) ?? new List<OBRate>();
            return RebuildForWorkShift(shift, rates);
        }

        // använd listan som skickas in
        public ObSummary RebuildForWorkShift(WorkShift workShift, IReadOnlyList<OBRate> obRates)
        {
            var summary = new ObSummary();

            if (workShift == null || workShift.JobProfileId <= 0 || workShift.Id <= 0)
                return summary;

            if (!workShift.StartTime.HasValue || !workShift.EndTime.HasValue)
                return summary;

            // 1) Rensa gamla events för passet
            _obEventRepository.DeleteForWorkShift(workShift.Id);

            var shiftStart = workShift.StartTime.Value;
            var shiftEnd = workShift.EndTime.Value;
            if (shiftEnd <= shiftStart)
                return summary;

            // Bara aktiva regler
            var rates = (obRates ?? Array.Empty<OBRate>())
                .Where(r => r != null && r.IsActive)
                .ToList();

            if (rates.Count == 0)
                return summary;

            // minut-scan + gruppera sammanhängande segment med samma (kategori + rate + tidsspann)
            OBRate current = null;
            DateTime segmentStart = shiftStart;

            void Flush(DateTime segmentEnd)
            {
                if (current == null)
                    return;
                if (segmentEnd <= segmentStart)
                { current = null; return; }

                SaveSegmentSplitByMidnight(segmentStart, segmentEnd, current, workShift, summary);
                current = null;
            }

            DateTime cursor = shiftStart;

            while (cursor < shiftEnd)
            {
                var next = cursor.AddMinutes(1);

                var match = FindBestMatchingRate(cursor, workShift, rates);

                // starta segment
                if (current == null && match != null)
                {
                    current = match;
                    segmentStart = cursor;
                }
                // fortsätt eller byt segment
                else if (current != null)
                {
                    bool same =
                        match != null &&
                        match.Category == current.Category &&
                        match.RatePerHour == current.RatePerHour &&
                        match.StartTime == current.StartTime &&
                        match.EndTime == current.EndTime;

                    if (!same)
                    {
                        Flush(cursor);
                        if (match != null)
                        {
                            current = match;
                            segmentStart = cursor;
                        }
                    }
                }

                // lämnar OB
                if (match == null && current != null)
                    Flush(cursor);

                cursor = next;
            }

            // flush sista
            if (current != null)
                Flush(shiftEnd);

            // runda
            summary.TotalObHours = Math.Round(summary.TotalObHours, 2);
            summary.TotalObPay = Math.Round(summary.TotalObPay, 2);
            summary.EveningHours = Math.Round(summary.EveningHours, 2);
            summary.NightHours = Math.Round(summary.NightHours, 2);
            summary.EveningPay = Math.Round(summary.EveningPay, 2);
            summary.NightPay = Math.Round(summary.NightPay, 2);

            return summary;
        }

        private void SaveSegmentSplitByMidnight(DateTime from, DateTime to, OBRate rateRule, WorkShift shift, ObSummary summary)
        {
            if (to <= from)
                return;

            // om samma datum: spara direkt
            if (from.Date == to.Date)
            {
                SaveEvent(from, to, rateRule, shift, summary);
                return;
            }

            // annars splitta vid midnatt
            var midnight = from.Date.AddDays(1);
            SaveEvent(from, midnight, rateRule, shift, summary);
            SaveEvent(midnight, to, rateRule, shift, summary);
        }

        private void SaveEvent(DateTime from, DateTime to, OBRate rateRule, WorkShift shift, ObSummary summary)
        {
            if (to <= from)
                return;

            var hours = (decimal)(to - from).TotalHours;
            if (hours <= 0)
                return;

            var ev = OBEvent.Create(
                jobProfileId: shift.JobProfileId,
                workShiftId: shift.Id,
                workDate: from.Date,
                startTime: from.TimeOfDay,
                endTime: to.TimeOfDay,
                obType: rateRule.Category,
                ratePerHour: rateRule.RatePerHour,
                notes: null
            );

            // Snapshot av regel-id + prioritet för spårbarhet i lönerevision.
            // Gör det enklare att felsöka “varför blev OB så här?” även efter regeländringar.
            ev.OBRateId = rateRule.Id;
            ev.Priority = rateRule.Priority;
            
            // Day type
            var date = from.Date;
            var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

            ev.DayType =
                shift.IsBigHoliday ? OBDayType.BigHoliday :
                shift.IsHoliday ? OBDayType.Holiday :
                isWeekend ? OBDayType.Weekend :
                OBDayType.Weekday;

            // sätt exakt (så totalsumma blir stabil)
            ev.Hours = Math.Round(hours, 2);
            ev.TotalAmount = Math.Round(ev.Hours * ev.RatePerHour, 2);

            _obEventRepository.Save(ev);

            // summary (total alltid)
            summary.TotalObHours += ev.Hours;
            summary.TotalObPay += ev.TotalAmount;

            // dina UI-fält (kväll/natt)
            if (ev.OBType == OBCategory.Evening)
            {
                summary.EveningHours += ev.Hours;
                summary.EveningPay += ev.TotalAmount;
                summary.EveningRate = Math.Max(summary.EveningRate, ev.RatePerHour);
            }
            else if (ev.OBType == OBCategory.Night)
            {
                summary.NightHours += ev.Hours;
                summary.NightPay += ev.TotalAmount;
                summary.NightRate = Math.Max(summary.NightRate, ev.RatePerHour);
            }
        }

        public Task RebuildForJobLastMonths(int jobProfileId, int monthsBack = 4)
        {
            // 4 månader bakåt räknat från NU (inkl nuvarande månad)
            var to = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);  // exklusiv
            var from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-monthsBack); // inkl

            // Hämta OB-regler en gång
            var obRates = _obRateRepository.GetOBRates(jobProfileId)
                .Where(r => r.IsActive)
                .ToList();

            // Hämta pass i perioden
            var shifts = _workShiftRepository.GetWorkShiftsForDateRange(jobProfileId, from, to)
                .Where(s => s.ShiftType == ShiftType.Regular)
                .Where(s => s.StartTime.HasValue && s.EndTime.HasValue)
                .ToList();

            foreach (var shift in shifts)
            {
                // Bygg om events för passet baserat på NUVARANDE regler
                var summary = RebuildForWorkShift(shift, obRates);

                // Uppdatera WorkShift så historik/UI stämmer
                shift.EveningHours = summary.EveningHours;
                shift.NightHours = summary.NightHours;
                shift.OBHours = summary.TotalObHours;

                shift.EveningOBRate = summary.EveningRate;
                shift.NightOBRate = summary.NightRate;

                shift.EveningOBPay = summary.EveningPay;
                shift.NightOBPay = summary.NightPay;

                shift.OBPay = summary.TotalObPay;
                shift.TotalPay = shift.RegularPay + shift.OBPay;

                _workShiftRepository.SaveWorkShift(shift);
            }

            return Task.CompletedTask;
        }

        #region Matching (Priority + DayType)

        private OBRate FindBestMatchingRate(DateTime dt, WorkShift shift, List<OBRate> rates)
        {
            var time = dt.TimeOfDay;
            var day = dt.DayOfWeek;

            var matches = rates.Where(r =>
                IsDayMatch(r, day, shift.IsHoliday, shift.IsBigHoliday) &&
                IsTimeInRange(r.StartTime, r.EndTime, time));

            return matches
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.RatePerHour)
                .ThenByDescending(r => r.Id)
                .FirstOrDefault();
        }

        private static bool IsDayMatch(OBRate rate, DayOfWeek dayOfWeek, bool isHoliday, bool isBigHoliday)
        {
            if (isBigHoliday && rate.BigHolidays)
                return true;

            if (isHoliday && rate.Holidays)
                return true;

            return dayOfWeek switch
            {
                DayOfWeek.Monday => rate.Monday,
                DayOfWeek.Tuesday => rate.Tuesday,
                DayOfWeek.Wednesday => rate.Wednesday,
                DayOfWeek.Thursday => rate.Thursday,
                DayOfWeek.Friday => rate.Friday,
                DayOfWeek.Saturday => rate.Saturday,
                DayOfWeek.Sunday => rate.Sunday,
                _ => false
            };
        }

        private static bool IsTimeInRange(TimeSpan start, TimeSpan end, TimeSpan time)
        {
            // SPECIAL: 00:00–00:00 (eller samma tid) = gäller hela dygnet
            if (start == end)
                return true;

            if (start < end)
                return time >= start && time < end;

            // över midnatt (t.ex 22:00–06:00)
            return time >= start || time < end;
        }

        #endregion
    }
}
