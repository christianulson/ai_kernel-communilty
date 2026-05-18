using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;

namespace KrnlAI.VisualStudio.Services;

public sealed record ChatMessage(string Role, string Content, DateTime Timestamp);

public sealed class ChatHistoryService
{
    private const string CollectionPath = "KrnlAI.ChatHistory";
    private const int MaxMessages = 100;

    public void SaveMessages(IReadOnlyList<ChatMessage> messages)
    {
        try
        {
            var store = GetStore();
            if (store is null) return;

            if (!store.CollectionExists(CollectionPath))
                store.CreateCollection(CollectionPath);

            var count = Math.Min(messages.Count, MaxMessages);
            store.SetInt32(CollectionPath, "Count", count);

            for (int i = 0; i < count; i++)
            {
                var msg = messages[i];
                var value = $"{msg.Timestamp:O}|{msg.Role}|{msg.Content}";
                store.SetString(CollectionPath, $"Message_{i}", value);
            }
        }
        catch
        {
        }
    }

    public List<ChatMessage> LoadMessages()
    {
        var result = new List<ChatMessage>();

        try
        {
            var store = GetStore();
            if (store is null) return result;

            if (!store.CollectionExists(CollectionPath))
                return result;

            var count = store.GetInt32(CollectionPath, "Count", 0);
            for (int i = 0; i < count; i++)
            {
                var value = store.GetString(CollectionPath, $"Message_{i}", "");
                var parts = value.Split(new[] { '|' }, 3);
                if (parts.Length == 3 &&
                    DateTime.TryParse(parts[0], out var ts) &&
                    !string.IsNullOrEmpty(parts[1]))
                {
                    result.Add(new ChatMessage(parts[1], parts[2], ts));
                }
            }
        }
        catch
        {
        }

        return result;
    }

    public void ClearHistory()
    {
        try
        {
            var store = GetStore();
            if (store is null) return;

            if (store.CollectionExists(CollectionPath))
                store.DeleteCollection(CollectionPath);
        }
        catch
        {
        }
    }

    private static WritableSettingsStore? GetStore()
    {
        try
        {
            var shellSettingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            return shellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
        }
        catch
        {
            return null;
        }
    }
}
