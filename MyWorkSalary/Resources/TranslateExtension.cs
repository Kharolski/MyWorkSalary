using Microsoft.Maui.Controls;
using System;
using System.ComponentModel;
using System.Globalization;

namespace MyWorkSalary.Resources
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

    public class TranslationManager : INotifyPropertyChanged
    {
        private static readonly Lazy<TranslationManager> lazy =
            new(() => new TranslationManager());
        public static TranslationManager Instance => lazy.Value;

        public string this[string key] =>
            Resources.Resx.Resources.ResourceManager.GetString(key, Resources.Resx.Resources.Culture);

        public void ChangeCulture(CultureInfo culture)
        {
            Resources.Resx.Resources.Culture = culture;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
