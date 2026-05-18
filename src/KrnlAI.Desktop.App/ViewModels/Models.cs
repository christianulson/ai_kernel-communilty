namespace KrnlAI.Desktop.App.ViewModels;

public record AgentInfo(string Id, string Name, string Description);

public record ConversationSession(string Id, string Title, DateTime CreatedAt);

public record PolicyDomain(string Name);