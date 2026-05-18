namespace KrnlAI.Desktop.Core.Services;

public interface IThemeService
{
    string CurrentTheme { get; }
    event EventHandler<string>? ThemeChanged;
    void SetTheme(string themeName);
    void ToggleTheme();
    IEnumerable<string> GetAvailableThemes();
}

public class ThemeService : IThemeService
{
    private string _currentTheme = "dark";
    private readonly HashSet<string> _availableThemes = new() { "dark", "light" };

    public string CurrentTheme => _currentTheme;
    public event EventHandler<string>? ThemeChanged;

    public void SetTheme(string themeName)
    {
        if (_availableThemes.Contains(themeName.ToLower()))
        {
            _currentTheme = themeName.ToLower();
            ThemeChanged?.Invoke(this, _currentTheme);
        }
    }

    public void ToggleTheme()
    {
        SetTheme(_currentTheme == "dark" ? "light" : "dark");
    }

    public IEnumerable<string> GetAvailableThemes() => _availableThemes;
}

public static class ThemeResources
{
    public static string DarkBackground => "#08111F";
    public static string DarkSurface => "#0E1727";
    public static string DarkBorder => "#1E293B";
    public static string DarkTextPrimary => "#E5EEFC";
    public static string DarkTextSecondary => "#8AA0BC";
    public static string DarkAccent => "#38BDF8";
    public static string DarkPrimary => "#38BDF8";
    public static string DarkError => "#FB7185";
    public static string DarkWarning => "#F59E0B";

    public static string LightBackground => "#FFFFFF";
    public static string LightSurface => "#F6F8FA";
    public static string LightBorder => "#D0D7DE";
    public static string LightTextPrimary => "#24292F";
    public static string LightTextSecondary => "#57606A";
    public static string LightAccent => "#0969DA";
    public static string LightPrimary => "#0969DA";
    public static string LightError => "#CF222E";
    public static string LightWarning => "#9A6700";
}
