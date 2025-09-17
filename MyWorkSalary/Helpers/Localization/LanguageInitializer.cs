using System.Globalization;
using MyWorkSalary.Models.Core;      // För AppSettings

namespace MyWorkSalary.Helpers.Localization
{
    public static class LanguageInitializer
    {
        /// <summary>
        /// Initierar appens språk vid uppstart
        /// </summary>
        public static void InitializeLanguage(AppSettings appSettings)
        {
            // 1. Om användaren redan har ett sparat språk → använd det
            if (!string.IsNullOrWhiteSpace(appSettings.LanguageCode))
            {
                var culture = new CultureInfo(appSettings.LanguageCode);
                TranslationManager.Instance.ChangeCulture(culture);
                System.Diagnostics.Debug.WriteLine($"[LANG] Använder sparat språk: {appSettings.LanguageCode}");
                return;
            }

            // 2. Om inget språk sparat → kolla systemets språk
            var deviceCulture = CultureInfo.CurrentUICulture;
            var supportedLanguages = new[] { "en", "sv" }; // Lägg till fler språk senare

            var cultureCode = supportedLanguages.Contains(deviceCulture.TwoLetterISOLanguageName)
                ? deviceCulture.TwoLetterISOLanguageName
                : "en"; // fallback → engelska

            // 3. Sätt språk i resx
            TranslationManager.Instance.ChangeCulture(new CultureInfo(cultureCode));

            // 4. Spara språk i AppSettings
            appSettings.LanguageCode = cultureCode;
            System.Diagnostics.Debug.WriteLine($"[LANG] Första start, sparar språk: {cultureCode}");
        }
    }
}
