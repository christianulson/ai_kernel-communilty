using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;

namespace KrnlAI.VisualStudio.Services;

public sealed class SettingsService : ISettingsService
{
    private const string CollectionPath = "KrnlAI";
    private WritableSettingsStore? _store;

    public string Endpoint { get; set; } = "http://localhost:65335";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public string? DefaultProvider { get; set; }
    public string? DefaultModel { get; set; }

    public void Load()
    {
        try
        {
            _store = CreateStore();
            if (_store is null) return;

            Endpoint = ReadString("Endpoint", "http://localhost:65335");
            TimeoutSeconds = ReadInt("TimeoutSeconds", 30);
            MaxRetries = ReadInt("MaxRetries", 3);
            DefaultProvider = ReadString("DefaultProvider", null);
            DefaultModel = ReadString("DefaultModel", null);
        }
        catch
        {
            // Fall back to defaults
        }
    }

    public void Save()
    {
        try
        {
            _store ??= CreateStore();
            if (_store is null) return;

            if (!_store.CollectionExists(CollectionPath))
                _store.CreateCollection(CollectionPath);

            _store.SetString(CollectionPath, "Endpoint", Endpoint);
            _store.SetInt32(CollectionPath, "TimeoutSeconds", TimeoutSeconds);
            _store.SetInt32(CollectionPath, "MaxRetries", MaxRetries);

            if (DefaultProvider is not null)
                _store.SetString(CollectionPath, "DefaultProvider", DefaultProvider);
            if (DefaultModel is not null)
                _store.SetString(CollectionPath, "DefaultModel", DefaultModel);
        }
        catch
        {
            // Silently fail on save
        }
    }

    private static WritableSettingsStore? CreateStore()
    {
        try
        {
            var shellSettingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            return shellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
        }
        catch
        {
            return null;
        }
    }

    private string ReadString(string key, string? defaultValue)
    {
        if (_store is null || !_store.PropertyExists(CollectionPath, key))
            return defaultValue ?? string.Empty;
        return _store.GetString(CollectionPath, key, defaultValue ?? string.Empty);
    }

    private int ReadInt(string key, int defaultValue)
    {
        if (_store is null || !_store.PropertyExists(CollectionPath, key))
            return defaultValue;
        return _store.GetInt32(CollectionPath, key, defaultValue);
    }
}
