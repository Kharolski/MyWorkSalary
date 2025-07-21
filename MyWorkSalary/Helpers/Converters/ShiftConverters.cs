using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;
using System.Collections.Concurrent;
using System.Globalization;

namespace MyWorkSalary.Helpers.Converters
{
    // 1. Konverterar WorkShift till ikon baserat på ShiftType och tid
    public class ShiftTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkShift shift)
            {
                // SPECIALHANTERING FÖR SEMESTER/SJUK/VAB
                return shift.ShiftType switch
                {
                    ShiftType.Vacation => "🏖️ Sem",
                    ShiftType.SickLeave => "🤒 Sjuk",
                    ShiftType.OnCall => "📞 Jour",
                    ShiftType.VAB => "👶 VAB",
                    ShiftType.Regular => GetTimeBasedIcon(shift),
                    _ => "📋 Pass"
                };
            }
            return "📋 Pass";
        }

        private string GetTimeBasedIcon(WorkShift shift)
        {
            // Om inga tider finns (säkerhetscheck)
            if (!shift.StartTime.HasValue)
                return "📋 Pass";

            var startHour = shift.StartTime.Value.Hour;

            // Nattpass: 21:00-07:00
            if (startHour >= 21 || startHour < 7)
                return "🌙 Natt";
            // Kvällspass: 16:00-21:00
            if (startHour >= 16 && startHour < 21)
                return "🌅 Kväll";
            // Dagpass: 06:00-16:00
            return "☀️ Dag";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 2. Konverterar WorkShift till datum-sträng
    public class ShiftToDateStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkShift shift)
            {
                var swedishCulture = new CultureInfo("sv-SE");

                // ANVÄND ShiftDate FÖR SEMESTER/SJUK
                if (shift.ShiftType == ShiftType.Vacation || shift.ShiftType == ShiftType.SickLeave)
                {
                    return shift.ShiftDate.ToString("dddd d MMMM", swedishCulture);
                }

                // Vanliga pass - använd StartTime om det finns
                if (shift.StartTime.HasValue)
                {
                    // För nattpass som går över midnatt - visa startdatum
                    if (shift.EndTime.HasValue && shift.StartTime.Value.Date != shift.EndTime.Value.Date)
                    {
                        return shift.StartTime.Value.ToString("dddd d MMMM", swedishCulture);
                    }
                    // Vanligt pass samma dag
                    return shift.StartTime.Value.ToString("dddd d MMMM", swedishCulture);
                }

                // Fallback till ShiftDate
                return shift.ShiftDate.ToString("dddd d MMMM", swedishCulture);
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 3. Konverterar WorkShift till tid-sträng eller period-info
    public class ShiftToTimeStringConverter : IValueConverter
    {
        private static IWorkShiftService _workShiftService;
        private static readonly ConcurrentDictionary<int, string> _descriptionCache = new();

        // Static event
        public static event Action<int> SickLeaveDescriptionUpdated;

        // Metod för DI
        public static void Initialize(IWorkShiftService workShiftService)
        {
            _workShiftService = workShiftService;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkShift shift)
            {
                // SPECIALHANTERING FÖR VAB
                if (shift.ShiftType == ShiftType.VAB)
                {
                    return GetVABDescription(shift);
                }

                // SPECIALHANTERING FÖR SEMESTER
                if (shift.ShiftType == ShiftType.Vacation)
                {
                    // Kolla om det är obetald semester
                    bool isUnpaidVacation = shift.Notes != null && shift.Notes.Contains("PlannedHours:");

                    return isUnpaidVacation ? "Obetald ledighet" : "Semester";
                }

                // SPECIALHANTERING FÖR SJUK - MED ASYNC
                if (shift.ShiftType == ShiftType.SickLeave)
                {
                    return GetSickLeaveDescription(shift);  // Async-hantering
                }

                // SPECIALHANTERING FÖR JOUR
                if (shift.ShiftType == ShiftType.OnCall)
                {
                    if (shift.StartTime.HasValue && shift.EndTime.HasValue)
                    {
                        string activeInfo = shift.TotalHours > 0
                            ? $"{shift.TotalHours:F1}t aktiv"
                            : "jour";
                        return $"{shift.StartTime.Value:HH:mm}→{shift.EndTime.Value:HH:mm} ({activeInfo})";
                    }
                    return "Jour - Heldag";
                }

                // VANLIGA PASS MED TIDER
                if (shift.StartTime.HasValue && shift.EndTime.HasValue)
                {
                    // För nattpass som går över midnatt
                    if (shift.StartTime.Value.Date != shift.EndTime.Value.Date)
                    {
                        return $"{shift.StartTime.Value:HH:mm} → {shift.EndTime.Value:HH:mm}";
                    }
                    // Vanligt pass samma dag
                    return $"{shift.StartTime.Value:HH:mm} → {shift.EndTime.Value:HH:mm}";
                }

                // Fallback för pass utan tider
                return "Ingen tid registrerad";
            }
            return "";
        }

        // Metod för async sjukbeskrivning
        private string GetSickLeaveDescription(WorkShift shift)
        {
            // 1. Kolla cache först
            if (_descriptionCache.TryGetValue(shift.Id, out var cachedResult))
            {
                return cachedResult;
            }

            // 2. Om ingen service, använd fallback
            if (_workShiftService == null)
            {
                return "Sjukskrivning";
            }

            // 3. Starta async-laddning i bakgrunden
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await _workShiftService.GetSickLeaveDescriptionAsync(shift.Id);

                    // Spara i cache
                    _descriptionCache[shift.Id] = result;

                    // Trigga UI-uppdatering
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        SickLeaveDescriptionUpdated?.Invoke(shift.Id);
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Fel vid sjukbeskrivning-laddning för WorkShift {shift.Id}: {ex.Message}");
                }
            });

            // 4. Returnera loading-state medan vi väntar
            return "Sjukskrivning...";
        }

        private string GetVABDescription(WorkShift shift)
        {
            // Parse VAB-data från Notes
            if (shift.Notes != null && shift.Notes.StartsWith("VABData:"))
            {
                try
                {
                    var data = shift.Notes.Replace("VABData:", "");
                    var parts = data.Split('|');

                    var scheduledPart = parts.FirstOrDefault(p => p.StartsWith("Scheduled="));
                    var workedPart = parts.FirstOrDefault(p => p.StartsWith("Worked="));
                    var isHourlyPart = parts.FirstOrDefault(p => p.StartsWith("IsHourly="));

                    if (scheduledPart != null && workedPart != null && isHourlyPart != null)
                    {
                        var scheduled = decimal.Parse(scheduledPart.Replace("Scheduled=", ""));
                        var worked = decimal.Parse(workedPart.Replace("Worked=", ""));
                        var isHourly = bool.Parse(isHourlyPart.Replace("IsHourly=", ""));

                        if (isHourly)
                        {
                            return "Vab - Timanställd";
                        }
                        else if (worked == 0)
                        {
                            return "Vab - Heldag";
                        }
                        else
                        {
                            var lostHours = scheduled - worked;
                            return $"Vab - Delvis (-{lostHours:F1}t)"; // Visa förlorade timmar i beskrivningen
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Fel vid VAB-beskrivning: {ex.Message}");
                }
            }

            // Fallback
            return "Vård av barn - Heldag";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ShiftToHoursDisplayConverter : IValueConverter
    {
        private static IWorkShiftService _workShiftService;
        private static readonly ConcurrentDictionary<int, string> _cache = new();

        // Static event för UI-uppdatering
        public static event Action<int> SickLeaveDataUpdated;

        // Metod för DI
        public static void Initialize(IWorkShiftService workShiftService)
        {
            _workShiftService = workShiftService;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkShift shift)
            {
                return shift.ShiftType switch
                {
                    ShiftType.OnCall => shift.TotalHours > 0
                        ? $"{shift.TotalHours:F1}t"
                        : "Jour",
                    ShiftType.SickLeave => GetSickLeaveHours(shift),  // Async-hantering
                    ShiftType.VAB => GetVABHours(shift),
                    ShiftType.Vacation => GetVacationHours(shift),
                    _ => $"{shift.TotalHours:F1}t"
                };
            }
            return "0t";
        }

        // Metod för async sjukdata
        private string GetSickLeaveHours(WorkShift shift)
        {
            // 1. Kolla cache först
            if (_cache.TryGetValue(shift.Id, out var cachedResult))
            {
                return cachedResult;
            }

            // 2. Om ingen service, använd fallback
            if (_workShiftService == null)
            {
                return "0t";
            }

            // 3. Starta async-laddning i bakgrunden
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await _workShiftService.GetSickLeaveHoursDisplayAsync(shift.Id);

                    // Spara i cache
                    _cache[shift.Id] = result;

                    // Trigga event på main thread 
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        SickLeaveDataUpdated?.Invoke(shift.Id);
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Fel vid sjukdata-laddning för WorkShift {shift.Id}: {ex.Message}");
                }
            });

            // 4. Returnera loading-state medan vi väntar
            return "...";
        }

        private string GetVacationHours(WorkShift shift)
        {
            // Betald semester
            if (shift.TotalHours > 0)
            {
                return $"{shift.TotalHours:F1}t";
            }

            // Obetald semester - parse PlannedHours från Notes
            if (shift.Notes != null && shift.Notes.Contains("PlannedHours:"))
            {
                var parts = shift.Notes.Split('|');
                var plannedPart = parts.FirstOrDefault(p => p.StartsWith("PlannedHours:"));
                if (plannedPart != null)
                {
                    var hoursText = plannedPart.Replace("PlannedHours:", "");
                    if (decimal.TryParse(hoursText, out decimal plannedHours))
                    {
                        return plannedHours > 0 ? $"-{plannedHours:F1}t" : "0t";
                    }
                }
            }

            // Fallback
            return "0t";
        }

        private string GetVABHours(WorkShift shift)
        {
            // Parse VAB-data från Notes
            if (shift.Notes != null && shift.Notes.StartsWith("VABData:"))
            {
                try
                {
                    var data = shift.Notes.Replace("VABData:", "");
                    var parts = data.Split('|');

                    var scheduledPart = parts.FirstOrDefault(p => p.StartsWith("Scheduled="));
                    var workedPart = parts.FirstOrDefault(p => p.StartsWith("Worked="));
                    var isHourlyPart = parts.FirstOrDefault(p => p.StartsWith("IsHourly="));

                    if (scheduledPart != null && workedPart != null && isHourlyPart != null)
                    {
                        var scheduled = decimal.Parse(scheduledPart.Replace("Scheduled=", ""));
                        var worked = decimal.Parse(workedPart.Replace("Worked=", ""));
                        var isHourly = bool.Parse(isHourlyPart.Replace("IsHourly=", ""));

                        if (isHourly)
                        {
                            return "VAB"; // Timanställd
                        }
                        else
                        {
                            if (worked == 0)
                            {
                                return "0t"; // Heldag VAB = 0 arbetade timmar
                            }
                            else
                            {
                                return $"{worked:F1}t"; // Delvis VAB = bara arbetade timmar
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Fel vid VAB-parsing: {ex.Message}");
                }
            }

            // Fallback
            return "VAB";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && parameter is string targetString)
            {
                return stringValue.Equals(targetString, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SickLeaveDataUpdatedMessage
    {
        public int WorkShiftId { get; set; }

        public SickLeaveDataUpdatedMessage(int workShiftId)
        {
            WorkShiftId = workShiftId;
        }
    }
}
