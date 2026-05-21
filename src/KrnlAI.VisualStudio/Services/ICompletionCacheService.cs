namespace KrnlAI.VisualStudio.Services;

public sealed record CachedCompletion(
    string Prefix,
    string[] Suggestions,
    double[] Scores,
    DateTime CachedAt
);

public interface ICompletionCacheService
{
    CachedCompletion? Get(string contextHash);
    void Set(string contextHash, CachedCompletion completion);
    void Invalidate(string contextHash);
    void Clear();
    int Count { get; }
}
