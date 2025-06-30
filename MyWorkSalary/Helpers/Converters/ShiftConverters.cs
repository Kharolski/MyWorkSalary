using System.Globalization;
using MyWorkSalary.Models;

namespace MyWorkSalary.Helpers.Converters
{
    // 1. Konverterar WorkShift till ikon baserat på tid
    public class ShiftTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkShift shift)
            {
                var startHour = shift.StartTime.Hour;

                // Nattpass: 21:00-07:00
                if (startHour >= 21 || startHour < 7)
                    return "🌙 Natt";

                // Kvällspass: 16:00-21:00
                if (startHour >= 16 && startHour < 21)
                    return "🌅 Kväll";

                // Dagpass: 06:00-16:00
                return "☀️ Dag";
            }

            return "📋 Pass";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 2. Konverterar WorkShift till datum-sträng
    public class ShiftToDateStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkShift shift)
            {
                var swedishCulture = new CultureInfo("sv-SE");

                // För nattpass som går över midnatt - visa startdatum
                if (shift.StartTime.Date != shift.EndTime.Date)
                {
                    return $"{shift.StartTime.ToString("dddd d MMMM", swedishCulture)}";
                }

                // Vanligt pass samma dag
                return shift.StartTime.ToString("dddd d MMMM", swedishCulture);
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 3. Konverterar WorkShift till tid-sträng
    public class ShiftToTimeStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkShift shift)
            {
                // För nattpass som går över midnatt
                if (shift.StartTime.Date != shift.EndTime.Date)
                {
                    var swedishCulture = new CultureInfo("sv-SE");
                    return $"{shift.StartTime:HH:mm} → {shift.EndTime:HH:mm}";
                }

                // Vanligt pass samma dag
                return $"{shift.StartTime:HH:mm} → {shift.EndTime:HH:mm}";
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 4. Kontrollerar om värde är större än 0
    public class IsGreaterThanZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
                return decimalValue > 0;

            if (value is double doubleValue)
                return doubleValue > 0;

            if (value is int intValue)
                return intValue > 0;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
