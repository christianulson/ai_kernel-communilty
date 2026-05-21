namespace KrnlAI.VisualStudio.Services;

public enum CloudMode { Auto, AlwaysCloud, AlwaysLocal }

public interface ICloudDelegationService
{
    CloudMode Mode { get; }
    bool IsUsingCloud { get; }
    Task<T> ExecuteWithFallbackAsync<T>(
        Func<Task<T>> localAction,
        Func<Task<T>> cloudAction,
        CancellationToken ct = default);
}
