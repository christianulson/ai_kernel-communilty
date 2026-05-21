namespace KrnlAI.Desktop.Core.Abstractions;

using KrnlAI.Desktop.Core.Services;

public interface ISessionPersistenceService
{
    SessionStore Load();
    void Save(SessionStore store);
    ConversationData CreateNewConversation(string title);
    ConversationData RenameConversation(ConversationData conversation, string newTitle);
    SessionStore DeleteConversation(SessionStore store, string conversationId);
}
