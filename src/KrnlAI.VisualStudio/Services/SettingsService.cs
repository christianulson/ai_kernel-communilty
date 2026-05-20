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
    public bool EnableInlineCompletions { get; set; } = true;
    public bool EnableCodeLens { get; set; } = true;
    public bool EnableHover { get; set; } = true;
    public bool EnableCodeActions { get; set; } = true;

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
            EnableInlineCompletions = ReadBool("EnableInlineCompletions", true);
            EnableCodeLens = ReadBool("EnableCodeLens", true);
            EnableHover = ReadBool("EnableHover", true);
            EnableCodeActions = ReadBool("EnableCodeActions", true);
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

            _store.SetBoolean(CollectionPath, "EnableInlineCompletions", EnableInlineCompletions);
            _store.SetBoolean(CollectionPath, "EnableCodeLens", EnableCodeLens);
            _store.SetBoolean(CollectionPath, "EnableHover", EnableHover);
            _store.SetBoolean(CollectionPath, "EnableCodeActions", EnableCodeActions);
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

    private bool ReadBool(string key, bool defaultValue)
    {
        if (_store is null || !_store.PropertyExists(CollectionPath, key))
            return defaultValue;
        return _store.GetBoolean(CollectionPath, key, defaultValue);
    }
}
