using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface IPolicyClient
{
    Task<PolicyListResponse> GetPoliciesAsync(string? domain = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<PolicyDetails?> GetPolicyAsync(string policyId, CancellationToken cancellationToken = default);
    Task<PolicyInfo?> CreatePolicyAsync(CreatePolicyRequest request, CancellationToken cancellationToken = default);
    Task<PolicyInfo?> UpdatePolicyAsync(string policyId, UpdatePolicyRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeletePolicyAsync(string policyId, CancellationToken cancellationToken = default);
}
