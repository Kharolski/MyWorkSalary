namespace MyWorkSalary.Helpers.Localization
{
    /// <summary>
    /// Gör att du kan skriva {resx:Translate Key} i XAML.
    /// </summary>
    [ContentProperty(nameof(Text))]
    public class TranslateExtension : IMarkupExtension<BindingBase>
    {
        public string Text { get; set; }

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
        {
            if (Text == null)
                return null;

            return new Binding
            {
                Mode = BindingMode.OneWay,
                Path = $"[{Text}]",
                Source = TranslationManager.Instance
            };
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) =>
            (this as IMarkupExtension<BindingBase>).ProvideValue(serviceProvider);
    }
}
