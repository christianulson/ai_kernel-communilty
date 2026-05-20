namespace KrnlAI.VisualStudio.Services;

public sealed record Policy(
    string Id,
    string Name,
    string Description,
    string Domain,
    bool IsActive,
    double Score
);

public interface IPoliciesService
{
    Task<IReadOnlyList<Policy>> GetPoliciesAsync(CancellationToken ct);
    Task<bool> TogglePolicyAsync(string policyId, bool active, CancellationToken ct);
}
