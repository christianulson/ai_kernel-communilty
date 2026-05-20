namespace KrnlAI.VisualStudio.Services;

public interface IGitService
{
    Task<string> StatusAsync(CancellationToken ct);
    Task<string> DiffAsync(CancellationToken ct);
    Task<string> LogAsync(int count = 10, CancellationToken ct = default);
    Task<string> BranchAsync(CancellationToken ct);
    Task<bool> CommitAsync(string message, CancellationToken ct);
    Task<string> ReviewPullRequestAsync(int prNumber, CancellationToken ct);
}
