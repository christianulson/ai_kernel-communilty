using KrnlAI.VisualStudio.Extensibility.Core.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrnlAI.VisualStudio.Extensibility.Core.Services;

/// <summary>A chat message persisted in history.</summary>
public sealed record ChatMessage(string Role, string Content, DateTime Timestamp);

/// <summary>
/// Chat history persistence backed by an <see cref="ISettingsStore"/> (portable).
/// </summary>
public sealed class ChatHistoryService
{
    private const string CollectionPath = "KrnlAI.ChatHistory";
    private const int MaxMessages = 100;
    private readonly ISettingsStore _store;
    private readonly ILogger<ChatHistoryService> _logger;

    /// <summary>Creates a new instance.</summary>
    public ChatHistoryService(ISettingsStore store, ILogger<ChatHistoryService>? logger = null)
    {
        _store = store;
        _logger = logger ?? NullLogger<ChatHistoryService>.Instance;
    }

    /// <summary>Saves up to <see cref="MaxMessages"/> messages.</summary>
    public void SaveMessages(IReadOnlyList<ChatMessage> messages)
    {
        try
        {
            if (!_store.CollectionExists(CollectionPath))
                _store.CreateCollection(CollectionPath);

            var count = Math.Min(messages.Count, MaxMessages);
            _store.SetInt32(CollectionPath, "Count", count);

            for (var i = 0; i < count; i++)
            {
                var msg = messages[i];
                var value = $"{msg.Timestamp:O}|{msg.Role}|{msg.Content}";
                _store.SetString(CollectionPath, $"Message_{i}", value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save chat history");
        }
    }

    /// <summary>Loads the persisted messages.</summary>
    public List<ChatMessage> LoadMessages()
    {
        var result = new List<ChatMessage>();

        try
        {
            if (!_store.CollectionExists(CollectionPath))
                return result;

            var count = _store.GetInt32(CollectionPath, "Count", 0);
            for (var i = 0; i < count; i++)
            {
                var value = _store.GetString(CollectionPath, $"Message_{i}", string.Empty);
                var parts = value.Split(['|'], 3);
                if (parts.Length == 3 &&
                    DateTime.TryParse(parts[0], out var ts) &&
                    !string.IsNullOrEmpty(parts[1]))
                {
                    result.Add(new ChatMessage(parts[1], parts[2], ts));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load chat history");
        }

        return result;
    }

    /// <summary>Clears the persisted history.</summary>
    public void ClearHistory()
    {
        try
        {
            if (_store.CollectionExists(CollectionPath))
                _store.DeleteCollection(CollectionPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear chat history");
        }
    }
}