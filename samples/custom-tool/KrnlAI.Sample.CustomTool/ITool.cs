namespace KrnlAI.Sample.CustomTool;

public interface ITool<in TInput, TOutput>
{
    string Name { get; }
    string Description { get; }
    Task<TOutput> ExecuteAsync(TInput input, CancellationToken ct = default);
}
