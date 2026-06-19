using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Services;

public sealed class ThemeManager : IDisposable
{
    private readonly IThemeService _themeService;
    private readonly Timer? _scheduleTimer;

    public ThemeManager(IThemeService themeService)
    {
        _themeService = themeService;
        _themeService.ThemeChanged += OnThemeChanged;
        ApplyTheme(_themeService.CurrentTheme);
        SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
        _scheduleTimer = new Timer(_ => CheckScheduledTheme(), null, TimeSpan.Zero, TimeSpan.FromMinutes(15));
    }

    private void CheckScheduledTheme()
    {
        try
        {
            var hour = DateTime.Now.Hour;
            var settings = ServiceLocator.Instance.SettingsService.LoadSettings();
            if (settings.AutoDarkMode == true)
            {
                var shouldBeDark = hour is < 6 or >= 18;
                var isDark = _themeService.CurrentTheme == "dark";
                if (shouldBeDark && !isDark) _themeService.SetTheme("dark");
                else if (!shouldBeDark && isDark) _themeService.SetTheme("light");
            }
        }
        catch { }
    }

    private void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.VisualStyle)
        {
            var usesLightTheme = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme", "1")?.ToString() == "1";
            _themeService.SetTheme(usesLightTheme ? "light" : "dark");
        }
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
