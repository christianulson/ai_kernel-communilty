using Kernel.Contracts;
using Refit;

namespace Kernel.Contracts.Abstractions;

public interface IKernelPolicyApi
{
    [Get("/policy/snapshot")]
    Task<PolicySnapshotDto> GetSnapshotAsync(string? domain = null, CancellationToken ct = default);

    [Get("/policy/versions")]
    Task<List<PolicyVersionDto>> GetVersionsAsync(string domain, string actionType, string scenario, int limit = 20, CancellationToken ct = default);

    [Get("/policy/rollbacks")]
    Task<List<PolicyRollbackAuditDto>> GetRollbacksAsync(string domain, string actionType, string scenario, int limit = 20, CancellationToken ct = default);

    [Get("/policy/rollbacks/by-operator")]
    Task<List<PolicyRollbackAuditDetailedDto>> GetRollbacksByOperatorAsync(string performedBy, int limit = 50, CancellationToken ct = default);

    [Post("/policy/rollback")]
    Task<PolicyEntryDto> RollbackAsync([Body] PolicyRollbackRequest request, string domain, string actionType, string scenario, CancellationToken ct = default);
}
