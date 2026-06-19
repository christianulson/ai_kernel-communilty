namespace KrnlAI.Desktop.Core.Models;

public sealed record ThreadInfo(
    string ThreadId,
    string? Title,
    DateTimeOffset CreatedAt,
    string Status);

public sealed record MessageInfo(
    string MessageId,
    string ThreadId,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    Dictionary<string, object>? Metadata);

public sealed record RunInfo(
    string RunId,
    string ThreadId,
    string Status,
    string? CurrentStep,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string? LastError);
