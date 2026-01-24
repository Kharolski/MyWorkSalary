using System.Globalization;

namespace MyWorkSalary.Helpers.Converters
{
    public class BoolToOpacityConverter : IValueConverter
    {
        public double TrueOpacity { get; set; } = 1.0;
        public double FalseOpacity { get; set; } = 0.25;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? TrueOpacity : FalseOpacity;

            return FalseOpacity;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
