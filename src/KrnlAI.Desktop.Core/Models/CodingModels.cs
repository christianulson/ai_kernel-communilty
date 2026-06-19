namespace KrnlAI.Desktop.Core.Models;

public sealed record CodingRequest(
    string Code,
    string? Language,
    string? Description,
    string? TestFramework);

public sealed record CodingResponse(
    string? Result,
    string? Explanation,
    bool Success,
    string? Error);

public sealed record CodingStatus(
    string CycleId,
    string Status,
    string? CurrentPhase,
    double Progress,
    string? Result,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
