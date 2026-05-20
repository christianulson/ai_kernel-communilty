namespace KrnlAI.VisualStudio.Services;

public sealed record ApplyEditResult(
    bool Approved,
    string? Diff,
    string? Error
);

public interface IApplyEditService
{
    Task<bool> PreviewAndApplyAsync(string diff, CancellationToken ct);
    Task<ApplyEditResult> PreviewDiffAsync(string diff, CancellationToken ct);
    Task<bool> ApplyAsync(string diff, CancellationToken ct);
    Task UndoAsync();
}
