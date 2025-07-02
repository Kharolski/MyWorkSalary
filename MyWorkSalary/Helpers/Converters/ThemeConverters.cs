using System.Globalization;

namespace MyWorkSalary.Helpers.Converters
{
    public class ThemeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDark)
            {
                return isDark ? "🌙" : "☀️";
            }
            return "☀️";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
