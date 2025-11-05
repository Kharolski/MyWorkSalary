using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;
using System.Globalization;

public class SickLeaveHoursConverter : AsyncValueConverter<WorkShift, string>
{
    private static IWorkShiftService _workShiftService;

    // Sätt service via dependency injection
    public static void Initialize(IWorkShiftService workShiftService)
    {
        _workShiftService = workShiftService;
    }

    protected override async Task<string> LoadDataAsync(WorkShift workShift, object parameter, CultureInfo culture)
    {
        if (_workShiftService == null)
            return $"{0}{LocalizationHelper.Translate("HoursAbbreviation")}";

        return workShift.ShiftType switch
        {
            ShiftType.SickLeave => await _workShiftService.GetSickLeaveHoursDisplayAsync(workShift.Id),
            ShiftType.OnCall => workShift.TotalHours > 0
                ? $"{workShift.TotalHours:F1}{LocalizationHelper.Translate("HoursAbbreviation")}"
                : LocalizationHelper.Translate("ShiftType_OnCallShift"),
            ShiftType.VAB => $"-8{LocalizationHelper.Translate("HoursAbbreviation")}",
            ShiftType.Vacation => $"8{LocalizationHelper.Translate("HoursAbbreviation")}",
            _ => $"{workShift.TotalHours:F1}{LocalizationHelper.Translate("HoursAbbreviation")}"
        };
    }

    protected override string GetCacheKey(WorkShift workShift, object parameter)
    {
        return $"hours_{workShift.Id}_{workShift.ShiftType}";
    }

    protected override string GetDefaultValue()
    {
        return "..."; // Visar loading-state
    }
}
