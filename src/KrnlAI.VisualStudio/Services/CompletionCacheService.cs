namespace KrnlAI.VisualStudio.Services;

public sealed class CompletionCacheService(int maxEntries = 1000, TimeSpan? ttl = null) : ICompletionCacheService
{
    private readonly TimeSpan _ttl = ttl ?? TimeSpan.FromMinutes(5);
    private readonly LinkedList<string> _accessOrder = new();
    private readonly Dictionary<string, (CachedCompletion Value, LinkedListNode<string> Node)> _cache = [];
    private readonly object _lock = new();

    public int Count
    {
        get { lock (_lock) return _cache.Count; }
    }

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

    public void Set(string contextHash, CachedCompletion completion)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(contextHash, out var existing))
            {
                _accessOrder.Remove(existing.Node);
                _cache.Remove(contextHash);
            }

            while (_cache.Count >= maxEntries)
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

    public void Invalidate(string contextHash)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(contextHash, out var entry))
                RemoveEntry(contextHash, entry.Node);
        }
    }

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
