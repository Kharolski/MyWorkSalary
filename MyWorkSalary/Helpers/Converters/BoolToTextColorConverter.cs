using System.Globalization;

namespace MyWorkSalary.Helpers.Converters
{
    public class BoolToTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasActiveJob)
            {
                // Om det finns aktivt jobb → blå text, annars grå
                return hasActiveJob ? Color.FromArgb("#2986cc") : Colors.Gray;
            }

            return Colors.Gray; // Default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
