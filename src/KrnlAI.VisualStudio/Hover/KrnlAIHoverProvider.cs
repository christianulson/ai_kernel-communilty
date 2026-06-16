using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace KrnlAI.VisualStudio.Hover;

[Export(typeof(IHoverDisplay))]
[Name("KrnlAI Hover Display")]
[ContentType("text")]
public sealed class KrnlAIHoverProvider : IHoverDisplay
{
    private static readonly LRUCache<string, string> _cache = new(50, TimeSpan.FromMinutes(5));

    public bool IsHoverVisible => false;

    public event EventHandler? VisibilityChanged { add { } remove { } }

    public void Dispose()
    {
    }

    public bool OnHover(ITextView textView, ITextBuffer textBuffer, SnapshotPoint position, MouseHoverEventArgs hoverEventArgs)
    {
        return false;
    }
}

internal sealed class LRUCache<TKey, TValue> where TKey : notnull
{
    private readonly int _maxEntries;
    private readonly TimeSpan _ttl;
    private readonly Dictionary<TKey, CacheEntry> _cache = new();
    private readonly LinkedList<TKey> _accessOrder = new();
    private readonly object _lock = new();

    private sealed record CacheEntry(TValue Value, DateTime CreatedAt, LinkedListNode<TKey> Node);

    public LRUCache(int maxEntries, TimeSpan ttl)
    {
        _maxEntries = maxEntries;
        _ttl = ttl;
    }

    public TValue? Get(TKey key)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(key, out var entry))
                return default;

            if (DateTime.UtcNow - entry.CreatedAt > _ttl)
            {
                RemoveEntry(key, entry.Node);
                return default;
            }

            _accessOrder.Remove(entry.Node);
            _accessOrder.AddFirst(entry.Node);
            return entry.Value;
        }
    }

    public void Set(TKey key, TValue value)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var existing))
            {
                _accessOrder.Remove(existing.Node);
                _cache.Remove(key);
            }

            while (_cache.Count >= _maxEntries)
            {
                var last = _accessOrder.Last;
                if (last is null) break;
                _cache.Remove(last.Value);
                _accessOrder.RemoveLast();
            }

            var node = _accessOrder.AddFirst(key);
            _cache[key] = new CacheEntry(value, DateTime.UtcNow, node);
        }
    }
}
