namespace MyWorkSalary.Helpers.Localization
{
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
