using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface IGoalClient
{
    Task<GoalListResponse> GetActiveGoalsAsync(CancellationToken cancellationToken = default);
    Task<GoalDetails?> GetGoalAsync(string goalId, CancellationToken cancellationToken = default);
    Task<GoalInfo?> CreateGoalAsync(CreateGoalRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateGoalStatusAsync(string goalId, string action, CancellationToken cancellationToken = default);
}
