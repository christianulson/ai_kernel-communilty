namespace KrnlAI.Desktop.Core.Models;

public sealed record SnapshotInfo(string SnapshotId, string Label, DateTime CreatedAt, long Size);
public sealed record ObjectiveInfo(string ObjectiveId, string Description, string Status, double Progress, int Priority, string? Deadline);
public sealed record ObjectiveDetail(string ObjectiveId, string Description, string Status, double Progress, List<TargetInfo> Targets);
public sealed record TargetInfo(string TargetId, string Description, double CurrentValue, double TargetValue, string Unit);
public sealed record InvestigationInfo(string CaseId, string Title, string Status, int EvidenceCount, DateTime CreatedAt);
