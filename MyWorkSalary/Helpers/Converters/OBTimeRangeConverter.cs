using MyWorkSalary.Models.Specialized;
using System.Globalization;

namespace MyWorkSalary.Helpers.Converters
{
    public class OBTimeRangeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OBRate obRate)
            {
                return $"{obRate.StartTime:hh\\:mm} - {obRate.EndTime:hh\\:mm}";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
