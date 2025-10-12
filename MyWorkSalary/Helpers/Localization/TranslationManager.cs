using System.ComponentModel;
using System.Globalization;

namespace MyWorkSalary.Helpers.Localization
{
    public class TranslationManager : INotifyPropertyChanged
    {
        private static readonly Lazy<TranslationManager> lazy =
            new(() => new TranslationManager());
        public static TranslationManager Instance => lazy.Value;

        // Indexer: hämtar översatt text från resurser
        public string this[string key] =>
            Resources.Resx.Resources.ResourceManager.GetString(key, Resources.Resx.Resources.Culture);

        // Den nuvarande kulturen i appen
        public CultureInfo CurrentCulture =>
            Resources.Resx.Resources.Culture
            ?? Thread.CurrentThread.CurrentUICulture
            ?? CultureInfo.CurrentCulture;

        // Format för DatePicker — hämtas direkt från aktuell kultur
        public string DatePickerFormat => CurrentCulture.DateTimeFormat.LongDatePattern;

        /// <summary>
        /// Byter appens språk + kultur live.
        /// Uppdaterar alla trådar, global kultur och UI-bindings.
        /// </summary>
        public void ChangeCulture(CultureInfo culture)
        {
            if (culture == null)
                return;

            // Uppdatera kultur i resx
            Resources.Resx.Resources.Culture = culture;

            // Sätt trådens och globala kultur (språk, valuta, datumformat)
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Tvinga MAUI att använda nya format (för t.ex. valuta & datum)
            Microsoft.Maui.Controls.Application.Current.MainPage.Dispatcher.Dispatch(() =>
            {
                // Uppdatera alla bindings som lyssnar på TranslationManager
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                CultureChanged?.Invoke(this, EventArgs.Empty);
            });

            System.Diagnostics.Debug.WriteLine($"[LANG] TranslationManager: Kultur ändrad till {culture.Name}");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler CultureChanged;
    }
}
