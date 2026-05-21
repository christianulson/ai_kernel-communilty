namespace KrnlAI.VisualStudio.Services;

public enum RiskLevel { Low, Medium, High, Critical }

public sealed record ApprovalResult(bool Approved, string? Comment, string? ModifiedAction);

public interface IApprovalService
{
    ApprovalMode Mode { get; }
    Task<ApprovalResult> RequestApprovalAsync(
        string actionDescription,
        string details,
        RiskLevel riskLevel,
        CancellationToken ct = default);
}
