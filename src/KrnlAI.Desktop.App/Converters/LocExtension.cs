using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Converters;

[MarkupExtensionReturnType(typeof(string))]
public class LocExtension : MarkupExtension
{
    public LocExtension() { }
    public LocExtension(string key) { Key = key; }

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key)) return string.Empty;
        try
        {
            var service = ServiceLocatorAccess.GetLocalizationService();
            if (service != null)
            {
                var value = service.GetString(Key);
                if (!string.IsNullOrEmpty(value)) return value;
            }
        }
        catch (Exception ex) { KrnlLogger.Write(ex); }
        return Key;
    }
}

public class LocalizationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = parameter as string ?? value as string;
        if (string.IsNullOrEmpty(key)) return string.Empty;
        try
        {
            var service = ServiceLocatorAccess.GetLocalizationService();
            if (service != null)
            {
                var result = service.GetString(key);
                if (!string.IsNullOrEmpty(result)) return result;
            }
        }
        catch (Exception ex) { KrnlLogger.Write(ex); }
        return key;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException("LocalizationConverter is one-way only");
}
