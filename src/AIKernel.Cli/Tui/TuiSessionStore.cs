using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;

namespace KrnlAI.Cli.Tui;

internal static class SessionJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}

public sealed class TuiSessionStore
{
    private readonly string _sessionsDir;
    private const int MaxSessions = 20;

    public TuiSessionStore()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _sessionsDir = Path.Combine(home, ".krnlai", "tui-sessions");
        if (!Directory.Exists(_sessionsDir))
            Directory.CreateDirectory(_sessionsDir);
    }

    public async Task SaveAsync(string label, List<ChatMessage> messages)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd_HHmmss");
        var fileName = $"{timestamp}_{SanitizeFileName(label)}.json";
        var filePath = Path.Combine(_sessionsDir, fileName);

        var session = new TuiSession
        {
            Id = Guid.NewGuid().ToString("N"),
            Label = label,
            CreatedAt = DateTimeOffset.UtcNow,
            MessageCount = messages.Count,
            Messages = messages
        };

        var json = JsonSerializer.Serialize(session, SessionJson.Options);
        await File.WriteAllTextAsync(filePath, json);

        TrimOldSessions();
    }

    public async Task<List<TuiSession>> ListAsync()
    {
        if (!Directory.Exists(_sessionsDir))
            return [];

        var sessions = new List<TuiSession>();
        foreach (var file in Directory.GetFiles(_sessionsDir, "*.json").OrderByDescending(f => f))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var session = JsonSerializer.Deserialize<TuiSession>(json, SessionJson.Options);
                if (session != null)
                    sessions.Add(session);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Failed to read TUI session file '{0}': {1}", file, ex.Message);
            }
        }
        return sessions;
    }

    public async Task<TuiSession?> LoadAsync(string sessionId)
    {
        var sessions = await ListAsync();
        return sessions.FirstOrDefault(s => s.Id == sessionId);
    }

    public async Task<bool> DeleteAsync(string sessionId)
    {
        if (!Directory.Exists(_sessionsDir))
            return false;

        foreach (var file in Directory.GetFiles(_sessionsDir, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var session = JsonSerializer.Deserialize<TuiSession>(json, SessionJson.Options);
                if (session?.Id == sessionId)
                {
                    File.Delete(file);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Failed to inspect TUI session file '{0}': {1}", file, ex.Message);
            }
        }
        return false;
    }

    public async Task<string> ExportAsync(string sessionId)
    {
        var session = await LoadAsync(sessionId);
        if (session == null) return "";
        return JsonSerializer.Serialize(session, SessionJson.Options);
    }

    public async Task<TuiSession?> ImportAsync(string json)
    {
        try
        {
            var session = JsonSerializer.Deserialize<TuiSession>(json, SessionJson.Options);
            if (session == null || string.IsNullOrWhiteSpace(session.Label))
                return null;

            session.Id = Guid.NewGuid().ToString("N");
            session.CreatedAt = DateTimeOffset.UtcNow;

            var fileName = $"{DateTimeOffset.UtcNow:yyyy-MM-dd_HHmmss}_{SanitizeFileName(session.Label)}.json";
            var filePath = Path.Combine(_sessionsDir, fileName);
            await File.WriteAllTextAsync(filePath, json);

            TrimOldSessions();
            return session;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("Failed to import TUI session: {0}", ex.Message);
            return null;
        }
    }

    private void TrimOldSessions()
    {
        if (!Directory.Exists(_sessionsDir)) return;

        var files = Directory.GetFiles(_sessionsDir, "*.json")
            .OrderByDescending(f => f)
            .ToList();

        if (files.Count <= MaxSessions) return;

        foreach (var file in files.Skip(MaxSessions))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Failed to delete old TUI session file '{0}': {1}", file, ex.Message);
            }
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
        return sanitized.Length > 40 ? sanitized[..40] : sanitized;
    }
}

public sealed class TuiSession
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public int MessageCount { get; set; }
    public List<ChatMessage> Messages { get; set; } = [];
}
