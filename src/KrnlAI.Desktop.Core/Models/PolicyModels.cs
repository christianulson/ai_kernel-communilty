namespace KrnlAI.Desktop.Core.Models;

public record PolicyInfo(
    string Id,
    string Name,
    string Domain,
    string Version,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsActive
);

public record PolicyDetails(
    string Id,
    string Name,
    string Domain,
    string Version,
    string Content,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsActive,
    List<PolicyVersion>? Versions
);

public record PolicyVersion(
    string Version,
    DateTime CreatedAt,
    string CreatedBy,
    string? ChangeNote
);

public record CreatePolicyRequest(
    string Name,
    string Domain,
    string Content
);

public record UpdatePolicyRequest(
    string Name,
    string Domain,
    string Content
);

public record PolicyListResponse(
    List<PolicyInfo> Policies,
    int TotalCount,
    int Page,
    int PageSize
);