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
                System.Diagnostics.Debug.WriteLine($"[LANG] Första start, sparar språk: {cultureCode}");
            }

            // 3️⃣ Matcha språk → kultur (inkl. valuta)
            var cultureMap = new Dictionary<string, string>
            {
                { "sv", "sv-SE" }, // Svenska → SEK (kr)
                { "en", "en-IE" }, // Engelska → Euro (Irland)
                // framtida exempel:
                // { "de", "de-DE" }, // Tyska → EUR
                // { "fr", "fr-FR" }, // Franska → EUR
                // { "pl", "pl-PL" }  // Polska → PLN
            };

            string fullCultureCode = cultureMap.ContainsKey(cultureCode)
                ? cultureMap[cultureCode]
                : "en-IE"; // fallback

            var cultureInfo = new CultureInfo(fullCultureCode);

            // 4️⃣ Sätt global kultur (både språk + valuta + datumformat)
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            // 5️⃣ Uppdatera resx och översättningar
            TranslationManager.Instance.ChangeCulture(cultureInfo);

            System.Diagnostics.Debug.WriteLine($"[LANG] Initierad kultur: {cultureInfo.Name} ({cultureInfo.DisplayName})");
        }
    }
}
