using System.ComponentModel.Composition;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace KrnlAI.VisualStudio.CodingAgent;

[Export(typeof(ITextViewCreationListener))]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.Editable)]
internal sealed class KrnlAIInlineCompletionHandler : ITextViewCreationListener
{
    [Import]
    private ITextStructureNavigatorSelectorService NavigatorService { get; set; } = null!;

    public void TextViewCreated(ITextView textView)
    {
        textView.Properties.GetOrCreateSingletonProperty(() => new KrnlAIInlineCompletionSession(textView));
    }
}

internal sealed class KrnlAIInlineCompletionSession
{
    private readonly ITextView _textView;
    private readonly HttpClient _http = new();
    private readonly CompletionCacheService _cache = new();
    private string _baseUrl = "http://localhost:5000";
    private CancellationTokenSource? _pendingCts;

    private static readonly HashSet<char> TriggerChars = new()
    {
        '.', ' ', '\n', '\t', '(', ')', '{', '}', ';', ':',
        '=', '+', '-', '*', '/', '>', '<', '!', '~', '&', '|', '%', ','
    };

    public KrnlAIInlineCompletionSession(ITextView textView)
    {
        _textView = textView;
        _textView.TextBuffer.Changed += (sender, e) => HandleTextBufferChanged(sender, e);
    }

    private void HandleTextBufferChanged(object sender, TextContentChangedEventArgs e)
    {
        if (e.Changes.Count == 0) return;

        var lastChange = e.Changes[e.Changes.Count - 1];
        if (lastChange.OldText.Length == 0 && lastChange.NewText.Length == 1)
        {
            var ch = lastChange.NewText[0];
            if (!TriggerChars.Contains(ch)) return;
        }

        _pendingCts?.Cancel();
        _pendingCts = new CancellationTokenSource();
        var token = _pendingCts.Token;

        var fireAndForget = Task.Run(async () =>
        {
            try { await ProcessTextChangeAsync(token); }
            catch (OperationCanceledException) { }
            catch { }
        }, token);
    }

    private async System.Threading.Tasks.Task ProcessTextChangeAsync(CancellationToken token)
    {
        var snapshot = _textView.TextSnapshot;
        var caret = _textView.Caret.Position.BufferPosition.Position;

        var line = snapshot.GetLineFromPosition(caret);
        var startLine = Math.Max(0, line.LineNumber - 10);
        var contextStart = snapshot.GetLineFromPosition(snapshot.GetLineFromLineNumber(startLine).Start).Start;
        var prefix = snapshot.GetText(contextStart, caret - contextStart);
        var language = snapshot.TextBuffer.ContentType.TypeName;

        var cached = _cache.Get(prefix, language);
        if (cached != null)
        {
            ShowSuggestion(cached[0]);
            return;
        }

        // Get file path from text document
        string filePath = "";
        Microsoft.VisualStudio.Text.ITextDocument? textDoc;
        if (snapshot.TextBuffer.Properties.TryGetProperty(typeof(Microsoft.VisualStudio.Text.ITextDocument), out textDoc) && textDoc != null)
        {
            filePath = textDoc.FilePath;
        }

        var requestJson = JsonSerializer.Serialize(new
        {
            codeContext = prefix.Length > 2000 ? prefix[^2000..] : prefix,
            language,
            filePath
        });

        try
        {
            var response = await _http.PostAsync(
                $"{_baseUrl.TrimEnd('/')}/api/coding/complete",
                new StringContent(requestJson, Encoding.UTF8, "application/json"),
                token);

            if (!response.IsSuccessStatusCode) return;

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CompletionResponse>(responseJson);

            if (result?.Completions == null || result.Completions.Count == 0) return;

            _cache.Set(prefix, language, result.Completions);
            ShowSuggestion(result.Completions[0]);
        }
        catch (OperationCanceledException) { }
        catch { }
    }

    private void ShowSuggestion(string suggestion)
    {
        if (string.IsNullOrEmpty(suggestion)) return;

        _textView.Properties["KrnlAI_Suggestion"] = suggestion;
    }

    public void SetBaseUrl(string url) => _baseUrl = url;

    private sealed record CompletionResponse([property: JsonPropertyName("completions")] List<string>? Completions);
}

internal sealed class CompletionCacheService
{
    private sealed record CacheEntry(string Prefix, string Language, List<string> Completions, DateTime CreatedAt);

    private readonly Dictionary<string, CacheEntry> _cache = new();
    private const int MaxEntries = 200;
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

    public List<string>? Get(string prefix, string language)
    {
        var key = $"{language}:{prefix.GetHashCode()}";
        if (_cache.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow - entry.CreatedAt < Ttl && entry.Prefix == prefix)
                return entry.Completions;
            _cache.Remove(key);
        }
        return null;
    }

    public void Set(string prefix, string language, List<string> completions)
    {
        if (_cache.Count >= MaxEntries)
        {
            var oldest = _cache.OrderBy(kv => kv.Value.CreatedAt).First().Key;
            _cache.Remove(oldest);
        }
        var key = $"{language}:{prefix.GetHashCode()}";
        _cache[key] = new CacheEntry(prefix, language, completions, DateTime.UtcNow);
    }

    public void Clear() => _cache.Clear();
}
