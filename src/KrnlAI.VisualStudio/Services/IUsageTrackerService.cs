namespace KrnlAI.VisualStudio.Services;

public sealed record UsageStats(
    int CommandInvocations,
    int AgentRuns,
    int TokensIn,
    int TokensOut,
    int Errors,
    TimeSpan SessionDuration,
    int ApiCalls
);

public interface IUsageTrackerService
{
    UsageStats Stats { get; }
    event Action<UsageStats>? StatsChanged;
    void TrackCommand(string command);
    void TrackAgentRun(int tokensIn, int tokensOut);
    void TrackError(string error);
    void TrackApiCall();
    Task SaveAsync(CancellationToken ct = default);
    Task ResetAsync(CancellationToken ct = default);
}
