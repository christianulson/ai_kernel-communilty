#if AUTOCODE_ENABLE_CODELENS
using System.ComponentModel.Composition;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace KrnlAI.VisualStudio.Completion;

[Export(typeof(ICompletionSourceProvider))]
[Name("KrnlAI Completion Source")]
[ContentType("text")]
public sealed class KrnlAICompletionSourceProvider : ICompletionSourceProvider
{
    public ICompletionSource? TryCreateCompletionSource(ITextBuffer textBuffer)
    {
        return new KrnlAICompletionSource(textBuffer);
    }
}

public sealed class KrnlAICompletionSource : ICompletionSource
{
    private readonly ITextBuffer _textBuffer;
    private static readonly CompletionSetCache _cache = new(50, TimeSpan.FromSeconds(30));

    public KrnlAICompletionSource(ITextBuffer textBuffer)
    {
        _textBuffer = textBuffer;
    }

    public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
    {
        var triggerPoint = session.GetTriggerPoint(_textBuffer.CurrentSnapshot);
        if (triggerPoint is null) return;

        var snapshot = _textBuffer.CurrentSnapshot;
        var line = triggerPoint.Value.GetContainingLine();
        var lineText = line.GetText().TrimStart();
        if (string.IsNullOrWhiteSpace(lineText) || lineText.Length < 2)
            return;

        var cacheKey = ComputeHash(snapshot.GetText(), triggerPoint.Value.Position);
        var cached = _cache.Get(cacheKey);
        if (cached is not null)
        {
            var completions = cached.Select(t => new Microsoft.VisualStudio.Language.Intellisense.Completion(t)).ToList();
            if (completions.Count > 0)
            {
                var set = new CompletionSet(
                    moniker: "KrnlAI",
                    displayName: "Krnl-AI",
                    applicableTo: _textBuffer.CurrentSnapshot.CreateTrackingSpan(
                        triggerPoint.Value.Position, 0, SpanTrackingMode.EdgeInclusive),
                    completions: completions,
                    description: null
                );
                completionSets.Add(set);
            }
            return;
        }
    }

    public void Dispose()
    {
    }

    private static string ComputeHash(string text, int position)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes($"{text}:{position}");
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash).Substring(0, 16);
    }
}

internal sealed class CompletionSetCache
{
    private readonly int _maxEntries;
    private readonly TimeSpan _ttl;
    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly LinkedList<string> _accessOrder = new();
    private readonly object _lock = new();

    private sealed record CacheEntry(string[] Value, DateTime CreatedAt, LinkedListNode<string> Node);

    public CompletionSetCache(int maxEntries, TimeSpan ttl)
    {
        _maxEntries = maxEntries;
        _ttl = ttl;
    }

    public string[]? Get(string key)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(key, out var entry))
                return null;

            if (DateTime.UtcNow - entry.CreatedAt > _ttl)
            {
                RemoveEntry(key, entry.Node);
                return null;
            }

            _accessOrder.Remove(entry.Node);
            _accessOrder.AddFirst(entry.Node);
            return entry.Value;
        }
    }

    public void Set(string key, string[] value)
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

    private void RemoveEntry(string key, LinkedListNode<string> node)
    {
        _cache.Remove(key);
        _accessOrder.Remove(node);
    }
}
#endif
