namespace MyWorkSalary.Helpers.Localization
{
    /// <summary>
    /// Hjälpklass för dropdowns med översatta alternativ.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LocalizedOption<T>
    {
        public T Value { get; set; }
        public string DisplayName { get; set; }
    }
}
