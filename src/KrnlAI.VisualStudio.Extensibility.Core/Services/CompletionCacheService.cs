namespace KrnlAI.VisualStudio.Extensibility.Core.Services;

/// <summary>A cached completion entry.</summary>
public sealed record CachedCompletion(string Text, DateTime CachedAt);

/// <summary>
/// LRU completion cache with TTL (portable).
/// </summary>
public sealed class CompletionCacheService
{
    private readonly TimeSpan _ttl;
    private readonly int _maxEntries;
    private readonly LinkedList<string> _accessOrder = new();
    private readonly Dictionary<string, (CachedCompletion Value, LinkedListNode<string> Node)> _cache = new();
    private readonly object _lock = new();

    /// <summary>Creates a new instance.</summary>
    public CompletionCacheService(int maxEntries = 1000, TimeSpan? ttl = null)
    {
        _maxEntries = Math.Max(1, maxEntries);
        _ttl = ttl ?? TimeSpan.FromMinutes(5);
    }

    /// <summary>Number of cached entries.</summary>
    public int Count
    {
        get { lock (_lock) return _cache.Count; }
    }

    /// <summary>Gets a cached completion or null.</summary>
    public CachedCompletion? Get(string contextHash)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(contextHash, out var entry))
                return null;

            if (DateTime.UtcNow - entry.Value.CachedAt > _ttl)
            {
                RemoveEntry(contextHash, entry.Node);
                return null;
            }

            _accessOrder.Remove(entry.Node);
            _accessOrder.AddFirst(entry.Node);
            return entry.Value;
        }
    }

    /// <summary>Stores a completion, evicting LRU entries beyond capacity.</summary>
    public void Set(string contextHash, CachedCompletion completion)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(contextHash, out var existing))
            {
                _accessOrder.Remove(existing.Node);
                _cache.Remove(contextHash);
            }

            while (_cache.Count >= _maxEntries)
            {
                var last = _accessOrder.Last;
                if (last is null) break;
                _cache.Remove(last.Value);
                _accessOrder.RemoveLast();
            }

            var node = _accessOrder.AddFirst(contextHash);
            _cache[contextHash] = (completion, node);
        }
    }

    /// <summary>Invalidates a single entry.</summary>
    public void Invalidate(string contextHash)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(contextHash, out var entry))
                RemoveEntry(contextHash, entry.Node);
        }
    }

    /// <summary>Clears all entries.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
            _accessOrder.Clear();
        }
    }

    private void RemoveEntry(string key, LinkedListNode<string> node)
    {
        _cache.Remove(key);
        _accessOrder.Remove(node);
    }
}