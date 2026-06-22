namespace KrnlAI.VisualStudio.Services;

public sealed record VsOperationCall(
    string Id,
    string Name,
    string? Arguments,
    VsOperationState State,
    string? Result,
    string? Error,
    long ElapsedMs,
    DateTime StartedAt,
    IReadOnlyList<VsOperationCall>? Children
);
