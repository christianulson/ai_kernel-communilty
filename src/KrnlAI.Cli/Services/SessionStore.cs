using System.Collections.Concurrent;
using System.Text.Json;

namespace KrnlAI.Cli.Services;

public sealed record CliSession(
    string Id,
    string Name,
    DateTimeOffset CreatedAt,
    Dictionary<string, object>? Data = null);

public sealed class InMemorySessionStore
{
    private readonly ConcurrentDictionary<string, CliSession> _sessions = new();

    public CliSession Create(string name)
    {
        var id = $"session-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}";
        var session = new CliSession(id, name, DateTimeOffset.UtcNow);
        _sessions[id] = session;
        return session;
    }

    public CliSession? Get(string id)
    {
        return _sessions.TryGetValue(id, out var session) ? session : null;
    }

    public IReadOnlyList<CliSession> List()
    {
        return _sessions.Values.OrderByDescending(s => s.CreatedAt).ToList();
    }

    public bool Delete(string id)
    {
        return _sessions.TryRemove(id, out _);
    }

    public string ExportJson(string id)
    {
        if (!_sessions.TryGetValue(id, out var session))
            throw new KeyNotFoundException($"Session '{id}' not found");
        return JsonSerializer.Serialize(session, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public CliSession ImportJson(string json)
    {
        var session = JsonSerializer.Deserialize<CliSession>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }) ?? throw new InvalidOperationException("Invalid session JSON");
        _sessions[session.Id] = session;
        return session;
    }
}
