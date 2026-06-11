using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KrnlAI.Desktop.App.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return false;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value != null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BoolToStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? "Online" : "Offline";
        return "Desconhecido";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status.ToLower() switch
            {
                "completed" or "success" or "online" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                "aborted" or "warning" or "running" or "processing" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                "error" or "danger" or "offline" => new SolidColorBrush(Color.FromRgb(251, 113, 133)),
                _ => new SolidColorBrush(Color.FromRgb(138, 160, 188))
            };
        }
        return new SolidColorBrush(Color.FromRgb(138, 160, 188));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ScoreToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double score;
        if (value is double d) score = d;
        else if (value is float f) score = f;
        else return new SolidColorBrush(Color.FromRgb(138, 160, 188));

        if (score >= 0.8) return new SolidColorBrush(Color.FromRgb(34, 197, 94));
        if (score >= 0.5) return new SolidColorBrush(Color.FromRgb(245, 158, 11));
        return new SolidColorBrush(Color.FromRgb(251, 113, 133));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class MoodToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string mood)
        {
            return mood.ToLower() switch
            {
                "success" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                "info" => new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                "warning" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                "danger" => new SolidColorBrush(Color.FromRgb(251, 113, 133)),
                _ => new SolidColorBrush(Color.FromRgb(138, 160, 188))
            };
        }
        return new SolidColorBrush(Color.FromRgb(138, 160, 188));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class PercentToWidthConverter : IValueConverter
{
    private const double MaxWidth = 400;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float f) return Math.Max(2, f * MaxWidth);
        if (value is double d) return Math.Max(2, d * MaxWidth);
        return 2.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ListeningButtonConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isListening)
            return isListening ? "Parar Escuta" : "Iniciar Escuta";

        return "Escuta";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return new SolidColorBrush(Color.FromRgb(59, 130, 246));
        return new SolidColorBrush(Color.FromRgb(71, 85, 105));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BoolToPanelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return new SolidColorBrush(Color.FromRgb(30, 58, 138));
        return new SolidColorBrush(Color.FromRgb(30, 41, 59));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class Base64ToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string base64 || string.IsNullOrEmpty(base64))
            return null!;

        try
        {
            var bytes = System.Convert.FromBase64String(base64);
            using var ms = new System.IO.MemoryStream(bytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null!;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
