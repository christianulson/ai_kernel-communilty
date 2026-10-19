using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface IGovernanceClient
{
    Task<List<GovernanceBudget>> GetAutonomyBudgetsAsync(CancellationToken ct = default);
    Task<GovernanceBudget?> GetApprovalMatrixAsync(CancellationToken ct = default);
}
