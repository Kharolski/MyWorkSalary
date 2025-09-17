namespace MyWorkSalary.Helpers
{
    public static class L
    {
        // key = namn på strängen i resx
        public static string _(string key)
        {
            return Resources.Resx.Resources.ResourceManager.GetString(key) ?? key;
        }
    }
}
