using System;
using System.ComponentModel;

namespace MyWorkSalary.Helpers.Calendar
{
    public class CalendarDay : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _hasConflict;

        public DateTime Date { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(DayColor));
                OnPropertyChanged(nameof(BackgroundColor));
                OnPropertyChanged(nameof(BorderColor));
                OnPropertyChanged(nameof(TextColor)); 
            }
        }

        public bool HasConflict
        {
            get => _hasConflict;
            set
            {
                _hasConflict = value;
                OnPropertyChanged(nameof(HasConflict));
                OnPropertyChanged(nameof(DayColor));
                OnPropertyChanged(nameof(BackgroundColor));
                OnPropertyChanged(nameof(BorderColor));
                OnPropertyChanged(nameof(TextColor)); 
            }
        }

        public bool IsToday { get; set; }
        public bool IsCurrentMonth { get; set; }

        public int WeekNumber { get; set; }
        public string DayNumber => IsCurrentMonth ? Date.Day.ToString() : "";

        public Color DayColor
        {
            get
            {
                if (HasConflict)
                    return Colors.Red;
                if (IsSelected)
                    return Colors.DarkGreen;
                if (IsToday)
                    return Colors.DodgerBlue;
                return Colors.LightGray;
            }
        }

        public Color BackgroundColor
        {
            get
            {
                if (HasConflict)
                    return Color.FromArgb("#FFCCCC"); // ljus röd
                if (IsSelected)
                    return Color.FromArgb("#90EE90"); // ljus grön
                if (IsToday)
                    return Color.FromArgb("#ADD8E6"); // ljus blå
                return Colors.Transparent;
            }
        }

        public Color BorderColor
        {
            get
            {
                if (HasConflict)
                    return Colors.Red;
                if (IsSelected)
                    return Colors.DarkGreen;
                if (IsToday)
                    return Colors.DarkBlue;
                return Colors.Transparent;
            }
        }

        public Color TextColor
        {
            get
            {
                // Valda eller idag = alltid vit text
                if (IsSelected || IsToday)
                    return Colors.Black;

                if (HasConflict)
                    return Colors.Black;

                // Kolla aktuell app-tema
                return Application.Current.UserAppTheme == AppTheme.Dark ? Colors.White : Colors.Black;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}