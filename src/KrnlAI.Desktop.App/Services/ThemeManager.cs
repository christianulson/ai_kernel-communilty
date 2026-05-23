using System.Windows;
using System.Windows.Media;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Services;

public sealed class ThemeManager : IDisposable
{
    private readonly IThemeService _themeService;

    public ThemeManager(IThemeService themeService)
    {
        _themeService = themeService;
        _themeService.ThemeChanged += OnThemeChanged;
        ApplyTheme(_themeService.CurrentTheme);
    }

    private void OnThemeChanged(object? sender, string themeName)
    {
        ApplyTheme(themeName);
    }

    private static void ApplyTheme(string themeName)
    {
        if (Application.Current == null) return;

        var fileName = string.Equals(themeName, "light", StringComparison.OrdinalIgnoreCase) ? "Light.xaml" : "Dark.xaml";
        var uri = new Uri(
            $"pack://application:,,,/KrnlAI.Desktop;component/Resources/Themes/{fileName}",
            UriKind.Absolute);

        try
        {
            var newTheme = new ResourceDictionary { Source = uri };
            var merged = Application.Current.Resources.MergedDictionaries;

            var existing = merged.FirstOrDefault(d =>
                d.Source != null &&
                d.Source.OriginalString.Contains("/Resources/Themes/"));

            if (existing != null)
                merged.Remove(existing);

            merged.Insert(0, newTheme);

            foreach (var key in newTheme.Keys)
            {
                var newValue = newTheme[key];
                if (newValue is SolidColorBrush newBrush)
                {
                    if (Application.Current.Resources[key] is SolidColorBrush oldBrush)
                    {
                        var mutable = oldBrush.Clone();
                        mutable.Color = newBrush.Color;
                        Application.Current.Resources[key] = mutable;
                        continue;
                    }
                }
                Application.Current.Resources[key] = newValue;
            }

            KrnlLogger.Write($"ThemeManager: switched to '{fileName}' ({newTheme.Count} resources)");
        }
        catch (Exception ex)
        {
            KrnlLogger.Write($"ThemeManager: failed to load '{uri}' — {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
    }
}
