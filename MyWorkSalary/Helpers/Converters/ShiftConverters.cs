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
                return shift.ShiftType switch
                {
                    ShiftType.Vacation => $"🏖️",     //  {LocalizationHelper.Translate("ShiftType_Vacation")}
                    ShiftType.SickLeave => $"🤒",    //  {LocalizationHelper.Translate("ShiftType_SickLeave")}
                    ShiftType.OnCall => $"📞",       //  {LocalizationHelper.Translate("ShiftType_OnCall")}
                    ShiftType.Regular => "🕒",
                    _ => $"📋"   //  {LocalizationHelper.Translate("ShiftType_Regular")}
                };
            }
            return $"📋";    //  {LocalizationHelper.Translate("ShiftType_Regular")}
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

    #region Extra pass
    public class ShiftToShowExtraShiftStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is WorkShift s && s.ShiftType == ShiftType.Regular && s.IsExtraShift;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ShiftToExtraShiftStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not WorkShift s)
                return "";

            if (s.ShiftType != ShiftType.Regular || !s.IsExtraShift)
                return "";

            var job = ActiveJobProvider.Current;
            if (job?.ExtraShiftEnabled == true)
                return LocalizationHelper.Translate("ExtraShift_Status_Active");

            return LocalizationHelper.Translate("ExtraShift_Status_DisabledInSettings");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ShiftToExtraShiftStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not WorkShift s || s.ShiftType != ShiftType.Regular || !s.IsExtraShift)
                return Colors.Transparent;

            var job = ActiveJobProvider.Current;
            if (job?.ExtraShiftEnabled == true)
                return Colors.Green;

            return Color.FromArgb("#FB8C00"); // orange
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    #endregion
}
