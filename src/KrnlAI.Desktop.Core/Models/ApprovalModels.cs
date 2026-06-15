namespace KrnlAI.Desktop.Core.Models;

public enum ApprovalStatus { Pending, Approved, Rejected, Expired, Escalated }

public sealed record ApprovalRequest(
    string RequestId,
    string ActionId,
    string ActionType,
    string Description,
    string? PayloadJson,
    double RiskScore,
    string[] RequiredApprovers,
    DateTimeOffset CreatedAt,
    DateTimeOffset Deadline,
    ApprovalStatus Status,
    IReadOnlyList<ApprovalResponse> Responses,
    string? AgentName = null,
    string? RequestedBy = null);

public sealed record ApprovalResponse(
    string ApproverId,
    string ApproverName,
    bool Approved,
    string? Comment,
    DateTimeOffset Timestamp);
