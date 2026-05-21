using System.Text.Json;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;

namespace KrnlAI.VisualStudio.Services;

public interface ISessionStorage
{
    string? Read(string key);
    void Write(string key, string value);
    bool Exists(string key);
    void DeleteCollection();
}

public sealed class VsSessionStorage : ISessionStorage
{
    private const string CollectionPath = "KrnlAI.Session";
    private Microsoft.VisualStudio.Settings.WritableSettingsStore? GetStore()
    {
        try
        {
            var shellSettingsManager = new Microsoft.VisualStudio.Shell.Settings.ShellSettingsManager(
                Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider);
            return shellSettingsManager.GetWritableSettingsStore(
                SettingsScope.UserSettings);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return null;
        }
    }

    public string? Read(string key)
    {
        var store = GetStore();
        if (store is null || !store.CollectionExists(CollectionPath) || !store.PropertyExists(CollectionPath, key))
            return null;
        return store.GetString(CollectionPath, key, "");
    }

    public void Write(string key, string value)
    {
        var store = GetStore();
        if (store is null) return;
        if (!store.CollectionExists(CollectionPath))
            store.CreateCollection(CollectionPath);
        store.SetString(CollectionPath, key, value);
    }

    public bool Exists(string key)
    {
        var store = GetStore();
        return store is not null && store.CollectionExists(CollectionPath) && store.PropertyExists(CollectionPath, key);
    }

    public void DeleteCollection()
    {
        var store = GetStore();
        if (store is not null && store.CollectionExists(CollectionPath))
            store.DeleteCollection(CollectionPath);
    }
}

public sealed class InMemorySessionStorage : ISessionStorage
{
    private readonly Dictionary<string, string> _data = new();
    private bool _collectionDeleted;

    public string? Read(string key) => _collectionDeleted ? null : _data.TryGetValue(key, out var val) ? val : null;
    public void Write(string key, string value) { _data[key] = value; _collectionDeleted = false; }
    public bool Exists(string key) => !_collectionDeleted && _data.ContainsKey(key);
    public void DeleteCollection() { _data.Clear(); _collectionDeleted = true; }
}

public sealed class SessionTeleportService : ISessionTeleportService
{
    private readonly ISessionStorage _storage;
    private const string StateKey = "SessionState";

    public SessionState? CurrentSession { get; private set; }

    public SessionTeleportService(ISessionStorage? storage = null)
    {
        _storage = storage ?? new VsSessionStorage();
    }

    public async Task SaveSessionAsync(SessionState state, CancellationToken ct = default)
    {
        CurrentSession = state;
        try
        {
            var json = JsonSerializer.Serialize(state);
            await Task.Run(() => _storage.Write(StateKey, json), ct);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
        }
    }

    public async Task<SessionState?> RestoreSessionAsync(CancellationToken ct = default)
    {
        try
        {
            var exists = await Task.Run(() => _storage.Exists(StateKey), ct);
            if (!exists) return null;

            var json = await Task.Run(() => _storage.Read(StateKey), ct);
            if (string.IsNullOrEmpty(json)) return null;

            var state = JsonSerializer.Deserialize<SessionState>(json ?? "");
            CurrentSession = state;
            return state;
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return null;
        }
    }

    public async Task ClearSessionAsync(CancellationToken ct = default)
    {
        CurrentSession = null;
        try
        {
            await Task.Run(() => _storage.DeleteCollection(), ct);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
        }
    }
}
