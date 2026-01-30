using System.Globalization;

namespace MyWorkSalary.Helpers.Localization
{
    /// <summary>
    /// Ansvarar för att skapa korrekta CultureInfo-objekt utifrån språkkoder.
    /// </summary>
    public static class CultureHelper
    {
        #region Culture Mapping
        public static string GetFullCultureCode(string languageCode)
        {
            return languageCode switch
            {
                "sv" => "sv-SE", // Svenska (Sverige)
                "en" => "en-IE", // Engelska (Irland, använder EUR)
                "ru" => "ru-RU", // Ryska (Ryssland)
                "no" => "nb-NO", // Norska (Bokmål)
                "da" => "da-DK", // Danska (Danmark)
                //"pl" => "pl-PL", // Polska
                _ => languageCode // fallback, t.ex. "fr-FR" om redan full
            };
        }

        /// <summary>
        /// Returnerar färdig CultureInfo baserat på språkkoden.
        /// </summary>
        public static CultureInfo GetCulture(string languageCode)
        {
            var fullCode = GetFullCultureCode(languageCode);
            return new CultureInfo(fullCode);
        }
        #endregion

    }
}
