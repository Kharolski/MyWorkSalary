using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;
using System.Collections.Concurrent;
using System.Globalization;

namespace MyWorkSalary.Helpers.Converters
{
    // Konverterar WorkShift till ikon baserat på ShiftType och tid
    public class ShiftTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkShift shift)
            {
                // SPECIALHANTERING FÖR SEMESTER/SJUK/VAB
                return shift.ShiftType switch
                {
                    ShiftType.Vacation => $"🏖️",     //  {LocalizationHelper.Translate("ShiftType_Vacation")}
                    ShiftType.SickLeave => $"🤒",    //  {LocalizationHelper.Translate("ShiftType_SickLeave")}
                    ShiftType.OnCall => $"📞",       //  {LocalizationHelper.Translate("ShiftType_OnCall")}
                    ShiftType.VAB => $"👶",          //  {LocalizationHelper.Translate("ShiftType_VAB")}
                    ShiftType.Regular => GetTimeBasedIcon(shift),
                    _ => $"📋"   //  {LocalizationHelper.Translate("ShiftType_Regular")}
                };
            }
            return $"📋";    //  {LocalizationHelper.Translate("ShiftType_Regular")}
        }

        /// <summary>
        /// Determines the appropriate icon based on the start time of the given work shift.
        /// </summary>
        /// <remarks>The method uses the start time of the shift to categorize it into night, evening, or
        /// day shifts, each represented by a specific icon. If the start time is not available, it defaults to a
        /// regular shift icon.</remarks>
        /// <param name="shift">The work shift for which to determine the icon. The shift must have a valid start time.</param>
        /// <returns>A string containing an icon and the translated shift type. Returns a night icon for shifts starting between
        /// 21:00 and 07:00, an evening icon for shifts starting between 16:00 and 21:00, a day icon for shifts starting
        /// between 06:00 and 16:00, and a regular icon if no specific time-based icon applies.</returns>
        private string GetTimeBasedIcon(WorkShift shift)
        {
            // Om inga tider finns (säkerhetscheck)
            if (!shift.StartTime.HasValue)
                return $"📋";    //  {LocalizationHelper.Translate("ShiftType_Regular")}

            var startHour = shift.StartTime.Value.Hour;

            // Nattpass: 21:00-07:00
            if (startHour >= 21 || startHour < 7)
                return $"🌙";    //  {LocalizationHelper.Translate("ShiftType_Night")}
            // Kvällspass: 16:00-21:00
            if (startHour >= 13 && startHour < 21)
                return $"🌅";    //  {LocalizationHelper.Translate("ShiftType_Evening")}
            if (startHour >= 6 && startHour < 16)
                return $"☀️";    //  {LocalizationHelper.Translate("ShiftType_Day")}

            return $"📋";       //  {LocalizationHelper.Translate("ShiftType_Regular")}
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Konverterar WorkShift till datum-sträng
    public class ShiftToDateStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkShift shift)
            {
                // ANVÄND ShiftDate FÖR SEMESTER/SJUK
                if (shift.ShiftType == ShiftType.Vacation || shift.ShiftType == ShiftType.SickLeave)
                {
                    return shift.ShiftDate.ToString("dddd d MMMM", CultureInfo.CurrentCulture);
                }

                // Vanliga pass - använd StartTime om det finns
                if (shift.StartTime.HasValue)
                {
                    // För nattpass som går över midnatt - visa startdatum
                    if (shift.EndTime.HasValue && shift.StartTime.Value.Date != shift.EndTime.Value.Date)
                    {
                        return shift.StartTime.Value.ToString("dddd d MMMM", CultureInfo.CurrentCulture);
                    }
                    // Vanligt pass samma dag
                    return shift.StartTime.Value.ToString("dddd d MMMM", CultureInfo.CurrentCulture);
                }

                // Fallback till ShiftDate
                return shift.ShiftDate.ToString("dddd d MMMM", CultureInfo.CurrentCulture);
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Konverterar WorkShift till tid-sträng eller period-info
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

                    return isUnpaidVacation 
                        ? LocalizationHelper.Translate("ShiftType_UnpaidLeave") 
                        : LocalizationHelper.Translate("ShiftType_Vacation");
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
                            ? $"{shift.TotalHours:F1}{LocalizationHelper.Translate("HoursAbbreviation")} {LocalizationHelper.Translate("ShiftType_Active")}"
                            : LocalizationHelper.Translate("ShiftType_OnCallShift");
                        return $"{shift.StartTime.Value:HH:mm}→{shift.EndTime.Value:HH:mm} ({activeInfo})";
                    }
                    return LocalizationHelper.Translate("ShiftType_OnCallFullDay");
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
                return LocalizationHelper.Translate("ShiftType_NoTime");
            }
            return "";
        }

        // Metod för async sjukbeskrivning
        private string GetSickLeaveDescription(WorkShift shift)
        {
            // Kolla cache först
            if (_descriptionCache.TryGetValue(shift.Id, out var cachedResult))
            {
                return cachedResult;
            }

            // Om ingen service, använd fallback
            if (_workShiftService == null)
            {
                return LocalizationHelper.Translate("SickLeave_Default");
            }

            // Starta async-laddning i bakgrunden
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
                    System.Diagnostics.Debug.WriteLine($"❌ Error loading sick leave for WorkShift {shift.Id}: {ex.Message}");
                }
            });

            // Returnera loading-state medan vi väntar
            return LocalizationHelper.Translate("SickLeave_Loading");
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
                            return LocalizationHelper.Translate("VAB_Employee");
                        }
                        else if (worked == 0)
                        {
                            return LocalizationHelper.Translate("VAB_FullDay");
                        }
                        else
                        {
                            var lostHours = scheduled - worked;
                            return string.Format(LocalizationHelper.Translate("VAB_PartialDay"), lostHours);
                            //return $"Vab - Delvis (-{lostHours:F1}t)"; // Visa förlorade timmar i beskrivningen
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ VAB description error: {ex.Message}");
                }
            }

            // Fallback
            return LocalizationHelper.Translate("VAB_Default");
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
                        ? $"{shift.TotalHours:F1}{LocalizationHelper.Translate("HoursAbbreviation")}"
                        : LocalizationHelper.Translate("HoursDisplay_OnCall"),
                    ShiftType.SickLeave => GetSickLeaveHours(shift),  // Async-hantering
                    ShiftType.VAB => GetVABHours(shift),
                    ShiftType.Vacation => GetVacationHours(shift),
                    _ => $"{shift.TotalHours:F1}{LocalizationHelper.Translate("HoursAbbreviation")}"
                };
            }
            return LocalizationHelper.Translate("HoursDisplay_ZeroHours");
        }

        // Metod för async sjukdata
        private string GetSickLeaveHours(WorkShift shift)
        {
            // Kolla cache först
            if (_cache.TryGetValue(shift.Id, out var cachedResult))
            {
                return cachedResult;
            }

            // Om ingen service, använd fallback
            if (_workShiftService == null)
            {
                return LocalizationHelper.Translate("HoursDisplay_ZeroHours");
            }

            // Starta async-laddning i bakgrunden
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
                    System.Diagnostics.Debug.WriteLine($"❌ {LocalizationHelper.Translate("HoursDisplay_Error")} vid sjukdata-laddning: {ex.Message}");
                }
            });

            // Returnera loading-state medan vi väntar
            return LocalizationHelper.Translate("HoursDisplay_Loading");
        }

        private string GetVacationHours(WorkShift shift)
        {
            // Betald semester
            if (shift.TotalHours > 0)
            {
                return $"{shift.TotalHours:F1}{LocalizationHelper.Translate("HoursAbbreviation")}";
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
                        return plannedHours > 0
                            ? $"-{plannedHours:F1}{LocalizationHelper.Translate("HoursAbbreviation")}"
                            : LocalizationHelper.Translate("HoursDisplay_ZeroHours");
                    }
                }
            }

            // Fallback
            return LocalizationHelper.Translate("HoursDisplay_ZeroHours");
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
                            return LocalizationHelper.Translate("HoursDisplay_VAB"); // Timanställd
                        }
                        else
                        {
                            return worked == 0
                                ? LocalizationHelper.Translate("HoursDisplay_ZeroHours")                // Heldag VAB = 0 arbetade timmar
                                : $"{worked:F1}{LocalizationHelper.Translate("HoursAbbreviation")}";    // Delvis VAB = bara arbetade timmar
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ {LocalizationHelper.Translate("HoursDisplay_Error")} vid VAB-parsing: {ex.Message}");
                }
            }

            // Fallback
            return LocalizationHelper.Translate("HoursDisplay_VAB");
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
