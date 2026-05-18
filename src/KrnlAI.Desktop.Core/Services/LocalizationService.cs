using System.Text.Json;

namespace KrnlAI.Desktop.Core.Services;

/// <summary>
/// Static accessor for LocalizationService, used by LocExtension at XAML compile time.
/// </summary>
public static class ServiceLocatorAccess
{
    private static ILocalizationService? _service;
    private static readonly object _lock = new();

    public static void SetLocalizationService(ILocalizationService service)
    {
        lock (_lock) { _service = service; }
    }

    public static ILocalizationService? GetLocalizationService()
    {
        return _service;
    }
}

public interface ILocalizationService
{
    string CurrentCulture { get; }
    event EventHandler<string>? CultureChanged;
    string GetString(string key);
    void SetCulture(string culture);
    IEnumerable<string> GetAvailableCultures();
}

public class LocalizationService : ILocalizationService
{
    private string _currentCulture = "pt-BR";
    private Dictionary<string, string> _strings = new();
    private readonly HashSet<string> _availableCultures = new() { "pt-BR", "en" };
    private const string DefaultCulture = "pt-BR";

    public string CurrentCulture => _currentCulture;
    public event EventHandler<string>? CultureChanged;

    public LocalizationService()
    {
        LoadStrings(_currentCulture);
    }

    public string GetString(string key)
    {
        if (_strings.TryGetValue(key, out var value))
            return value;
        return $"[{key}]";
    }

    public void SetCulture(string culture)
    {
        if (_availableCultures.Contains(culture) && _currentCulture != culture)
        {
            _currentCulture = culture;
            LoadStrings(culture);
            CultureChanged?.Invoke(this, culture);
        }
    }

    public IEnumerable<string> GetAvailableCultures() => _availableCultures;

    private void LoadStrings(string culture)
    {
        try
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var filePath = Path.Combine(basePath, "Resources", "Strings", $"{culture}.json");

            if (!File.Exists(filePath))
            {
                // Try to find from the source directory during development
                var altPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..",
                    "KrnlAI.Desktop.App", "Resources", "Strings", $"{culture}.json");
                if (File.Exists(altPath))
                    filePath = altPath;
                else
                {
                    // Fallback to default
                    if (culture != DefaultCulture)
                    {
                        var defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Strings", $"{DefaultCulture}.json");
                        if (File.Exists(defaultPath))
                            filePath = defaultPath;
                    }
                }
            }

            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                    _strings = dict;
            }
        }
        catch
        {
            // Keep existing strings on error
        }
    }
}
