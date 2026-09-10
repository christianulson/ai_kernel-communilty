namespace KrnlAI.VisualStudio.Extensibility.Core.Services.Settings;

/// <summary>Abstraction over a key-value settings store (replaces VS WritableSettingsStore).</summary>
public interface ISettingsStore
{
    /// <summary>Returns whether the collection exists.</summary>
    bool CollectionExists(string collection);

    /// <summary>Creates a collection.</summary>
    void CreateCollection(string collection);

    /// <summary>Deletes a collection.</summary>
    void DeleteCollection(string collection);

    /// <summary>Sets an integer value.</summary>
    void SetInt32(string collection, string key, int value);

    /// <summary>Gets an integer value with a default.</summary>
    int GetInt32(string collection, string key, int defaultValue);

    /// <summary>Sets a string value.</summary>
    void SetString(string collection, string key, string value);

    /// <summary>Gets a string value with a default.</summary>
    string GetString(string collection, string key, string defaultValue);
}

/// <summary>In-memory settings store (tests and fallback).</summary>
public sealed class MemorySettingsStore : ISettingsStore
{
    private readonly Dictionary<string, Dictionary<string, object>> _collections = new();

    /// <inheritdoc/>
    public bool CollectionExists(string collection) => _collections.ContainsKey(collection);

    /// <inheritdoc/>
    public void CreateCollection(string collection)
    {
        if (!_collections.ContainsKey(collection))
            _collections[collection] = new Dictionary<string, object>();
    }

    /// <inheritdoc/>
    public void DeleteCollection(string collection) => _collections.Remove(collection);

    /// <inheritdoc/>
    public void SetInt32(string collection, string key, int value)
    {
        EnsureCollection(collection)[key] = value;
    }

    /// <inheritdoc/>
    public int GetInt32(string collection, string key, int defaultValue)
    {
        return _collections.TryGetValue(collection, out var entries)
            && entries.TryGetValue(key, out var value)
            && value is int i
                ? i
                : defaultValue;
    }

    /// <inheritdoc/>
    public void SetString(string collection, string key, string value)
    {
        EnsureCollection(collection)[key] = value;
    }

    /// <inheritdoc/>
    public string GetString(string collection, string key, string defaultValue)
    {
        return _collections.TryGetValue(collection, out var entries)
            && entries.TryGetValue(key, out var value)
            && value is string s
                ? s
                : defaultValue;
    }

    private Dictionary<string, object> EnsureCollection(string collection)
    {
        if (!_collections.TryGetValue(collection, out var entries))
        {
            entries = new Dictionary<string, object>();
            _collections[collection] = entries;
        }

        return entries;
    }
}