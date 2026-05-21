namespace KrnlAI.VisualStudio.Services;

public sealed record SessionState(
    string? SessionId,
    string? LastGoal,
    string[] ContextFiles,
    int MessageCount,
    DateTime LastActivityAt
);

public interface ISessionTeleportService
{
    SessionState? CurrentSession { get; }
    Task SaveSessionAsync(SessionState state, CancellationToken ct = default);
    Task<SessionState?> RestoreSessionAsync(CancellationToken ct = default);
    Task ClearSessionAsync(CancellationToken ct = default);
}
