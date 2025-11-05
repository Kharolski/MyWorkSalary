using System.Globalization;
using MyWorkSalary.Models.Core;      // För AppSettings

namespace MyWorkSalary.Helpers.Localization
{
    /// <summary>
    /// Väljer språk vid appstart (utifrån inställning eller systemets språk).
    /// </summary>
    public static class LanguageInitializer
    {
        /// <summary>
        /// Initierar appens språk vid uppstart
        /// </summary>
        public static void InitializeLanguage(AppSettings appSettings)
        {
            string languageCode;

            // Använd sparat språk om det finns
            if (!string.IsNullOrWhiteSpace(appSettings.LanguageCode))
            {
                languageCode = appSettings.LanguageCode;
            }
            else
            {
                // Kolla systemets språk första gången
                var deviceCulture = CultureInfo.CurrentUICulture;
                var supportedLanguages = LanguageProvider.GetSupportedLanguageCodes();

                languageCode = supportedLanguages.Contains(deviceCulture.TwoLetterISOLanguageName)
                    ? deviceCulture.TwoLetterISOLanguageName
                    : "en"; // fallback

                appSettings.LanguageCode = languageCode;
            }

            // Hämta fullständig kultur via helper
            var culture = CultureHelper.GetCulture(languageCode);

            // Sätt global kultur för språk, valuta och datum
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Uppdatera översättningarna i resx-filerna
            TranslationManager.Instance.ChangeCulture(culture);

        }
    }
}
