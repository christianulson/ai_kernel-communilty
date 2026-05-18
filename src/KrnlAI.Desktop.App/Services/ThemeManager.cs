using System.Windows;
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

        var uri = new Uri(
            $"pack://application:,,,/KrnlAI.Desktop;component/Resources/Themes/{themeName}.xaml",
            UriKind.Absolute);

        try
        {
            var themeDict = new ResourceDictionary { Source = uri };
            var merged = Application.Current.Resources.MergedDictionaries;

            var existing = merged.FirstOrDefault(d =>
                d.Source != null &&
                d.Source.OriginalString.Contains("/Resources/Themes/"));

            if (existing != null)
                merged.Remove(existing);

            merged.Insert(0, themeDict);
        }
        catch
        {
            // Fallback: theme file not found, keep current
        }
    }

    public void Dispose()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
    }
}
