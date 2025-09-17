using System.ComponentModel;
using System.Globalization;

namespace MyWorkSalary.Helpers.Localization
{
    public class TranslationManager : INotifyPropertyChanged
    {
        private static readonly Lazy<TranslationManager> lazy =
            new(() => new TranslationManager());
        public static TranslationManager Instance => lazy.Value;

        public string this[string key] =>
            Resources.Resx.Resources.ResourceManager.GetString(key, Resources.Resx.Resources.Culture);

        public void ChangeCulture(CultureInfo culture)
        {
            Resources.Resx.Resources.Culture = culture;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
