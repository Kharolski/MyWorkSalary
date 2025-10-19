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
            string cultureCode;

            // Använd sparat språk om det finns
            if (!string.IsNullOrWhiteSpace(appSettings.LanguageCode))
            {
                cultureCode = appSettings.LanguageCode;
            }
            else
            {
                // Kolla systemets språk första gången
                var deviceCulture = CultureInfo.CurrentUICulture;

                var supportedLanguages = new[] { "en", "sv" }; // kan byggas ut senare

                cultureCode = supportedLanguages.Contains(deviceCulture.TwoLetterISOLanguageName)
                    ? deviceCulture.TwoLetterISOLanguageName
                    : "en"; // fallback

                appSettings.LanguageCode = cultureCode;
            }

            // Skapa en neutral kultur (utan landsvaluta)
            // "en" istället för "en-IE", "sv" istället för "sv-SE"
            var cultureInfo = new CultureInfo(cultureCode);

            // Sätt språk (utan att påverka valutainställning)
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            // Uppdatera resx och översättningar
            TranslationManager.Instance.ChangeCulture(cultureInfo);

        }
    }
}
