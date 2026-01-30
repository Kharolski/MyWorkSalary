using MyWorkSalary.Models.Templates;
using System.Globalization;

namespace MyWorkSalary.Helpers.Converters
{
    public class SelectedTemplateTextColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var current = values.Length > 0 ? values[0] as OBRateTemplate : null;
            var selected = values.Length > 1 ? values[1] as OBRateTemplate : null;

            var isSelected = current != null && selected != null && ReferenceEquals(current, selected);

            // Om selected: använd mörk text även i dark theme (för att funka på ljusgrön)
            if (isSelected)
                return Colors.Black;

            // annars: normal textfärg beroende på tema
            return Application.Current?.RequestedTheme == AppTheme.Dark
                ? (Color)(Application.Current.Resources["White"])
                : (Color)(Application.Current.Resources["TextPrimary"]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
