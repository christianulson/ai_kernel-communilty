using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrnlAI.VisualStudio.Extensibility.Core.Services.Settings;

/// <summary>
/// Persistent <see cref="ISettingsStore"/> backed by a JSON file.
/// Defaults to <c>%LocalAppData%/KrnlAI/settings.json</c>.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _filePath;
    private readonly ILogger<JsonSettingsStore> _logger;
    private Dictionary<string, Dictionary<string, JsonElement>> _data = new();
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true
    };

    /// <summary>Creates a new instance, loading existing values from disk.</summary>
    public JsonSettingsStore(string? filePath = null, ILogger<JsonSettingsStore>? logger = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KrnlAI",
            "settings.json");
        _logger = logger ?? NullLogger<JsonSettingsStore>.Instance;
        Load();
    }

    /// <inheritdoc/>
    public bool CollectionExists(string collection) => _data.ContainsKey(collection);

    /// <inheritdoc/>
    public void CreateCollection(string collection)
    {
        if (!_data.ContainsKey(collection))
        {
            _data[collection] = new Dictionary<string, JsonElement>();
            Save();
        }
    }

    /// <inheritdoc/>
    public void DeleteCollection(string collection)
    {
        if (_data.Remove(collection))
            Save();
    }

    /// <inheritdoc/>
    public void SetInt32(string collection, string key, int value)
    {
        SetElement(collection, key, JsonSerializer.SerializeToElement(value));
    }

    /// <inheritdoc/>
    public int GetInt32(string collection, string key, int defaultValue)
    {
        if (TryGetElement(collection, key, out var element) && element.TryGetInt32(out var value))
            return value;

        return defaultValue;
    }

    /// <inheritdoc/>
    public void SetString(string collection, string key, string value)
    {
        SetElement(collection, key, JsonSerializer.SerializeToElement(value));
    }

    /// <inheritdoc/>
    public string GetString(string collection, string key, string defaultValue)
    {
        if (TryGetElement(collection, key, out var element) && element.ValueKind == JsonValueKind.String)
            return element.GetString() ?? defaultValue;

        return defaultValue;
    }

    private void SetElement(string collection, string key, JsonElement value)
    {
        if (!_data.TryGetValue(collection, out var entries))
        {
            entries = new Dictionary<string, JsonElement>();
            _data[collection] = entries;
        }

        entries[key] = value;
        Save();
    }

    private bool TryGetElement(string collection, string key, out JsonElement element)
    {
        if (_data.TryGetValue(collection, out var entries) && entries.TryGetValue(key, out element))
            return true;

        element = default;
        return false;
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return;

            var json = File.ReadAllText(_filePath);
            _data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(json, JsonOpts)
                ?? new Dictionary<string, Dictionary<string, JsonElement>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {Path}", _filePath);
            _data = new Dictionary<string, Dictionary<string, JsonElement>>();
        }
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir is not null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(_filePath, JsonSerializer.Serialize(_data, JsonOpts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {Path}", _filePath);
        }
    }
}