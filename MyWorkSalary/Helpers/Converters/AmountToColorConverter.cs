using System.Globalization;

namespace MyWorkSalary.Helpers.Converters
{
    public class AmountToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                if (decimalValue > 0)
                    return Application.Current.RequestedTheme == AppTheme.Dark
                        ? Application.Current.Resources["SalaryPositiveDark"]
                        : Application.Current.Resources["SalaryPositiveLight"];

                if (decimalValue < 0)
                    return Application.Current.RequestedTheme == AppTheme.Dark
                        ? Application.Current.Resources["SalaryNegativeDark"]
                        : Application.Current.Resources["SalaryNegativeLight"];
            }

            return Application.Current.RequestedTheme == AppTheme.Dark
                ? Application.Current.Resources["SalaryNeutralDark"]
                : Application.Current.Resources["SalaryNeutralLight"];
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
