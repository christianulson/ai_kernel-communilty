using System.Text.Json;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Services;

public sealed record ConversationData(
    int Version,
    string Id,
    string Title,
    List<ChatMessage> Messages,
    DateTime CreatedAt,
    DateTime? LastActivityAt
);

public sealed record SessionStore(
    int Version,
    List<ConversationData> Conversations,
    string? ActiveConversationId
);

public sealed class SessionPersistenceService : ISessionPersistenceService
{
    private const int CurrentVersion = 2;
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SessionPersistenceService(string? basePath = null)
    {
        var dir = basePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KrnlAI");
        _filePath = Path.Combine(dir, "sessions.json");
    }

    public SessionStore Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new SessionStore(CurrentVersion, new List<ConversationData>(), null);

            var json = File.ReadAllText(_filePath);
            var store = JsonSerializer.Deserialize<SessionStore>(json, JsonOpts);

            if (store is null)
                return new SessionStore(CurrentVersion, new List<ConversationData>(), null);

            return Migrate(store);
        }
        catch
        {
            return new SessionStore(CurrentVersion, new List<ConversationData>(), null);
        }
    }

    public void Save(SessionStore store)
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir is not null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var migrated = store with { Version = CurrentVersion };
            var json = JsonSerializer.Serialize(migrated, JsonOpts);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Silent fail on save
        }
    }

    public ConversationData CreateNewConversation(string title)
    {
        return new ConversationData(
            CurrentVersion,
            Guid.NewGuid().ToString("N"),
            title,
            new List<ChatMessage>(),
            DateTime.UtcNow,
            null
        );
    }

    public ConversationData RenameConversation(ConversationData conversation, string newTitle)
    {
        return conversation with { Title = newTitle };
    }

    public SessionStore DeleteConversation(SessionStore store, string conversationId)
    {
        var conversations = store.Conversations
            .Where(c => c.Id != conversationId)
            .ToList();
        var activeId = store.ActiveConversationId == conversationId ? null : store.ActiveConversationId;
        return store with { Conversations = conversations, ActiveConversationId = activeId };
    }

    private static SessionStore Migrate(SessionStore store)
    {
        if (store.Version >= CurrentVersion)
            return store;

        var version = store.Version;

        if (version < 2)
        {
            var migrated = store.Conversations
                .Select(c => c with
                {
                    Title = string.IsNullOrEmpty(c.Title)
                        ? GenerateTitle(c.Messages)
                        : c.Title
                })
                .ToList();
            store = store with { Version = 2, Conversations = migrated };
        }

        return store;
    }

    private static string GenerateTitle(List<ChatMessage> messages)
    {
        var firstUser = messages.FirstOrDefault(m => m.Role == MessageRole.User);
        if (firstUser is not null)
        {
            var text = firstUser.Content;
            return text.Length > 50 ? text[..50] + "..." : text;
        }
        return "New Conversation";
    }
}
