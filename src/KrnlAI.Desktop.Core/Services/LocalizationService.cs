using System.ComponentModel;
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

/// <summary>
/// Observable localized string bound to an <see cref="ILocalizationService"/>;
/// notifies on culture changes so bindings refresh automatically.
/// </summary>
public sealed class LocalizedStringValue : INotifyPropertyChanged
{
    private readonly ILocalizationService _service;
    private readonly string _key;

    /// <summary>Creates a new instance.</summary>
    public LocalizedStringValue(ILocalizationService service, string key)
    {
        _service = service;
        _key = key;
        _service.CultureChanged += (_, _) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }

    /// <summary>Localized value for the current culture.</summary>
    public string Value => _service.GetString(_key);

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;
}

public class LocalizationService : ILocalizationService
{
    private string _currentCulture = "pt-BR";
    private Dictionary<string, string> _strings = [];
    private readonly HashSet<string> _availableCultures = ["pt-BR", "en"];
    private const string DefaultCulture = "pt-BR";

    public string CurrentCulture => _currentCulture;
    public event EventHandler<string>? CultureChanged;

    public LocalizationService()
    {
        _currentCulture = ResolveInitialCulture();
        LoadStrings(_currentCulture);
    }

    private static string ResolveInitialCulture()
    {
        var env = Environment.GetEnvironmentVariable("KRNL_LANG");
        if (!string.IsNullOrWhiteSpace(env))
        {
            var normalized = env.Trim().ToLowerInvariant() switch
            {
                "pt" or "pt-br" => "pt-BR",
                "en" or "en-us" => "en",
                _ => null
            };
            if (normalized is not null)
                return normalized;
        }

        return DefaultCulture;
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
