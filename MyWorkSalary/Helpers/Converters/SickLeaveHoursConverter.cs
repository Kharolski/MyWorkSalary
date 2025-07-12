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
            return "0t";

        return workShift.ShiftType switch
        {
            ShiftType.SickLeave => await _workShiftService.GetSickLeaveHoursDisplayAsync(workShift.Id),
            ShiftType.OnCall => workShift.TotalHours > 0 ? $"{workShift.TotalHours:F1}t" : "Jour",
            ShiftType.VAB => "-8t",
            ShiftType.Vacation => "8t",
            _ => $"{workShift.TotalHours:F1}t"
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
