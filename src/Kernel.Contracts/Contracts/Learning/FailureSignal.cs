namespace LLMGateway.Api.Contracts.Learning;

public sealed record FailureSignal(
    string FailureType,
    string Tool,
    string? Goal,
    string Domain,
    int OccurrenceCount,
    DateTimeOffset LastSeenAt
);
