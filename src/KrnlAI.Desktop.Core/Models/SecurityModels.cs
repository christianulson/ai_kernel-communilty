namespace KrnlAI.Desktop.Core.Models;

public record SecurityIncident(
    string Id,
    string Description,
    string Severity,
    string Status,
    DateTimeOffset DetectedAt);

public record NotificationItem(
    string Id,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    DateTimeOffset CreatedAt);

public record GovernanceBudget(
    string Domain,
    double BudgetUsed,
    double BudgetLimit,
    bool IsExceeded);

public record ProvenanceEntry(
    string LinkId,
    string EntityType,
    string EntityId,
    string Description,
    bool IsIntact,
    DateTimeOffset RecordedAt);
