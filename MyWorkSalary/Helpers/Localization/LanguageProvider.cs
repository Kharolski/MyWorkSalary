using MyWorkSalary.ViewModels;
using System.Collections.ObjectModel;

namespace MyWorkSalary.Helpers.Localization
{
    /// <summary>
    /// Ger en lista över alla språk som användaren kan välja i appen.
    /// </summary>
    public static class LanguageProvider
    {
        #region Public API

        public static ObservableCollection<LanguageOption> GetAvailableLanguages()
        {
            return new ObservableCollection<LanguageOption>
            {
                new LanguageOption { DisplayName = Resources.Resx.Resources.LanguageEnglish, Code = "en" },
                new LanguageOption { DisplayName = Resources.Resx.Resources.LanguageSwedish, Code = "sv" },
                //new LanguageOption { DisplayName = Resources.Resx.Resources.LanguageNorwegian, Code = "no" },
                //new LanguageOption { DisplayName = Resources.Resx.Resources.LanguageDanish, Code = "da" },
                //new LanguageOption { DisplayName = Resources.Resx.Resources.LanguagePolish, Code = "pl" },
                //new LanguageOption { DisplayName = Resources.Resx.Resources.LanguageRussian, Code = "ru" },
                //new LanguageOption { DisplayName = Resources.Resx.Resources.LanguageFrench, Code = "fr" }
            };
        }

        public static IEnumerable<string> GetSupportedLanguageCodes()
        {
            yield return "en";
            yield return "sv";
            //yield return "no";
            //yield return "da";
            //yield return "pl";
            //yield return "ru";
            //yield return "fr";
        }

        #endregion
    }
}
