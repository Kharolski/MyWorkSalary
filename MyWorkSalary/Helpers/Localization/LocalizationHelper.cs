namespace MyWorkSalary.Helpers.Localization
{
    public static class LocalizationHelper
    {
        public static event Action LanguageChanged;

        // key = namn på strängen i resx
        // Enkel sträng
        public static string Translate(string key) =>
            Resources.Resx.Resources.ResourceManager.GetString(key) ?? key;

        // formaterad sträng med parametrar
        public static string Translate(string key, params object[] args)
        {
            var format = Resources.Resx.Resources.ResourceManager.GetString(key) ?? key;
            return string.Format(format, args);
        }

        public static void NotifyLanguageChanged()
        {
            LanguageChanged?.Invoke();
        }
    }
}
