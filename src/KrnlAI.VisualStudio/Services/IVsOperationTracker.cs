namespace KrnlAI.VisualStudio.Services;

public interface IVsOperationTracker
{
    event Action<VsOperationCall>? OperationStarted;
    event Action<VsOperationCall>? OperationCompleted;

    VsOperationScope Start(string name, string? arguments = null);
    IReadOnlyList<VsOperationCall> History { get; }
    void Clear();
}
