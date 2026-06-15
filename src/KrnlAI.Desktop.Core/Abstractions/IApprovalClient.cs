using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface IApprovalClient
{
    Task<List<ApprovalRequest>> GetPendingApprovalsAsync(string? role = null, CancellationToken ct = default);
    Task<ApprovalRequest?> GetApprovalDetailAsync(string requestId, CancellationToken ct = default);
    Task<ApprovalRequest?> ApproveRequestAsync(string requestId, string? comment = null, CancellationToken ct = default);
    Task<ApprovalRequest?> RejectRequestAsync(string requestId, string? comment = null, CancellationToken ct = default);
}
