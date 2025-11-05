using MyWorkSalary.Helpers.Localization;
using System.Globalization;

namespace MyWorkSalary.Helpers
{
    public static class FormatHelper
    {
        public static string FormatHours(decimal hours)
        {
            return $"{hours.ToString("0.0", CultureInfo.CurrentCulture)}{LocalizationHelper.Translate("HoursAbbreviation")}";
        }
    }
}
