using MyWorkSalary.Models.Templates;
using System.Globalization;

namespace MyWorkSalary.Helpers.Converters
{
    public class SelectedTemplateBackgroundConverter : IMultiValueConverter
    {
        // values[0] = current item
        // values[1] = selected item
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var current = values.Length > 0 ? values[0] as OBRateTemplate : null;
            var selected = values.Length > 1 ? values[1] as OBRateTemplate : null;

            // Selected -> lugn grön
            if (current != null && selected != null && ReferenceEquals(current, selected))
                return Color.FromArgb("#DBF7D9"); // DBF7D9

            // Unselected -> använd samma som du hade innan (light/dark)
            var key = Application.Current?.RequestedTheme == AppTheme.Dark
                ? "OBRuleBackgroundDark"
                : "OBRuleBackground";

            if (Application.Current?.Resources.TryGetValue(key, out var resource) == true && resource is Color c)
                return c;

            // sista fallback om nyckeln saknas
            return Colors.Transparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
