using MyWorkSalary.Helpers.Localization;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyWorkSalary.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private bool _isBusy;
        public bool IsBusy 
        { 
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        // Datum UK/EU format
        public string DatePickerFormat => TranslationManager.Instance.DatePickerFormat;

        // 24h format
        public string TimePickerFormat => "HH:mm";

        #region Constructor
        protected BaseViewModel()
        {
            TranslationManager.Instance.CultureChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(DatePickerFormat));
                OnPropertyChanged(nameof(TimePickerFormat));
            };
        }
        #endregion

        #region Property Changed
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

    }
}
