using System.Globalization;
using MyWorkSalary.Models;

namespace MyWorkSalary.Helpers.Converters
{
    // 1. Konverterar WorkShift till ikon baserat på ShiftType och tid
    public class ShiftTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkShift shift)
            {
                // ✅ SPECIALHANTERING FÖR SEMESTER/SJUK
                return shift.ShiftType switch
                {
                    ShiftType.Vacation => "🏖️ Sem",
                    ShiftType.SickLeave => "🤒 Sjuk",
                    ShiftType.Training => "📚 Utb",
                    ShiftType.OnCall => "📞 Jour",
                    ShiftType.Overtime => "⏰ Övertid",
                    ShiftType.Regular => GetTimeBasedIcon(shift),
                    _ => "📋 Pass"
                };
            }
            return "📋 Pass";
        }

        private string GetTimeBasedIcon(WorkShift shift)
        {
            // Om inga tider finns (säkerhetscheck)
            if (!shift.StartTime.HasValue)
                return "📋 Pass";

            var startHour = shift.StartTime.Value.Hour;

            // Nattpass: 21:00-07:00
            if (startHour >= 21 || startHour < 7)
                return "🌙 Natt";
            // Kvällspass: 16:00-21:00
            if (startHour >= 16 && startHour < 21)
                return "🌅 Kväll";
            // Dagpass: 06:00-16:00
            return "☀️ Dag";
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

                // ANVÄND ShiftDate FÖR SEMESTER/SJUK
                if (shift.ShiftType == ShiftType.Vacation || shift.ShiftType == ShiftType.SickLeave)
                {
                    return shift.ShiftDate.ToString("dddd d MMMM", swedishCulture);
                }

                // Vanliga pass - använd StartTime om det finns
                if (shift.StartTime.HasValue)
                {
                    // För nattpass som går över midnatt - visa startdatum
                    if (shift.EndTime.HasValue && shift.StartTime.Value.Date != shift.EndTime.Value.Date)
                    {
                        return shift.StartTime.Value.ToString("dddd d MMMM", swedishCulture);
                    }
                    // Vanligt pass samma dag
                    return shift.StartTime.Value.ToString("dddd d MMMM", swedishCulture);
                }

                // Fallback till ShiftDate
                return shift.ShiftDate.ToString("dddd d MMMM", swedishCulture);
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 3. Konverterar WorkShift till tid-sträng eller period-info
    public class ShiftToTimeStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkShift shift)
            {
                // SPECIALHANTERING FÖR SEMESTER/SJUK
                if (shift.ShiftType == ShiftType.Vacation)
                {
                    var days = shift.NumberOfDays ?? 1;
                    return $"Semester - {days} dag{(days > 1 ? "ar" : "")}";
                }

                if (shift.ShiftType == ShiftType.SickLeave)
                {
                    var days = shift.NumberOfDays ?? 1;
                    string karensInfo = shift.IsKarensDay ? " (inkl. karensdag)" : "";
                    return $"Sjukskrivning - {days} dag{(days > 1 ? "ar" : "")}{karensInfo}";
                }

                if (shift.ShiftType == ShiftType.Training)
                {
                    if (shift.NumberOfDays.HasValue)
                    {
                        var days = shift.NumberOfDays.Value;
                        return $"Utbildning - {days} dag{(days > 1 ? "ar" : "")}";
                    }
                }

                // VANLIGA PASS MED TIDER
                if (shift.StartTime.HasValue && shift.EndTime.HasValue)
                {
                    // För nattpass som går över midnatt
                    if (shift.StartTime.Value.Date != shift.EndTime.Value.Date)
                    {
                        return $"{shift.StartTime.Value:HH:mm} → {shift.EndTime.Value:HH:mm}";
                    }
                    // Vanligt pass samma dag
                    return $"{shift.StartTime.Value:HH:mm} → {shift.EndTime.Value:HH:mm}";
                }

                // Fallback för pass utan tider
                return "Ingen tid registrerad";
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
