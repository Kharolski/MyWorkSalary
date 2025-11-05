namespace MyWorkSalary.Helpers.Localization
{
    public static class LocalizationHelper
    {
        public static event Action LanguageChanged;

        // key = namn på strängen i resx
        public static string Translate(string key) =>
            Resources.Resx.Resources.ResourceManager.GetString(key) ?? key;

        public static void NotifyLanguageChanged()
        {
            LanguageChanged?.Invoke();
        }
    }
}
