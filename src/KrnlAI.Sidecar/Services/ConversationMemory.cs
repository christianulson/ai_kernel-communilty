using System.Collections.Concurrent;

namespace KrnlAI.Sidecar.Services;

public sealed record Message(string Role, string Content, DateTimeOffset Timestamp);

public sealed record ConversationContext
{
    public string SessionId { get; init; } = Guid.NewGuid().ToString("N");
    public string? UserName { get; set; }
    public readonly List<Message> Messages = [];
    public readonly Dictionary<string, string> KnownFacts = [];
    public DateTimeOffset LastActivity { get; set; } = DateTimeOffset.UtcNow;

    public void AddMessage(string role, string content)
    {
        Messages.Add(new Message(role, content, DateTimeOffset.UtcNow));
        LastActivity = DateTimeOffset.UtcNow;
    }

    public string GetRecentHistory(int count = 10)
    {
        var recent = Messages.TakeLast(count);
        return string.Join("\n", recent.Select(m => $"[{m.Role}] {m.Content}"));
    }
}

public interface IConversationStore
{
    ConversationContext GetOrCreate(string sessionId);
    void AddFact(string sessionId, string key, string value);
    string? GetFact(string sessionId, string key);
    void CleanupStale(TimeSpan maxAge);
}

public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, ConversationContext> _sessions = new(StringComparer.OrdinalIgnoreCase);

    public ConversationContext GetOrCreate(string sessionId)
    {
        return _sessions.GetOrAdd(sessionId, _ => new ConversationContext { SessionId = sessionId });
    }

    public void AddFact(string sessionId, string key, string value)
    {
        var ctx = GetOrCreate(sessionId);
        ctx.KnownFacts[key] = value;
    }

    public string? GetFact(string sessionId, string key)
    {
        if (_sessions.TryGetValue(sessionId, out var ctx))
            return ctx.KnownFacts.TryGetValue(key, out var val) ? val : null;
        return null;
    }

    public void CleanupStale(TimeSpan maxAge)
    {
        var cutoff = DateTimeOffset.UtcNow - maxAge;
        foreach (var kv in _sessions)
        {
            if (kv.Value.LastActivity < cutoff)
                _sessions.TryRemove(kv.Key, out _);
        }
    }
}
