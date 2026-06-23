namespace KrnlAI.VisualStudio.Services;

public static class VsGlobalTracker
{
    public static VsOperationTracker Instance { get; } = new();
}

public sealed class VsOperationTracker : IVsOperationTracker
{
    private readonly List<VsOperationCall> _history = [];
    private readonly object _lock = new();
    private int _counter;

    public event Action<VsOperationCall>? OperationStarted;
    public event Action<VsOperationCall>? OperationCompleted;

    public IReadOnlyList<VsOperationCall> History
    {
        get
        {
            lock (_lock)
            {
                return [.. _history];
            }
        }
    }

    public VsOperationScope Start(string name, string? arguments = null)
    {
        var id = $"op-{Interlocked.Increment(ref _counter)}";
        var scope = new VsOperationScope(this, id, name, arguments);

        var startedOp = new VsOperationCall(
            Id: id,
            Name: name,
            Arguments: arguments,
            State: VsOperationState.Running,
            Result: null,
            Error: null,
            ElapsedMs: 0,
            StartedAt: DateTime.UtcNow,
            Children: null
        );

        lock (_lock)
        {
            _history.Add(startedOp);
        }

        OperationStarted?.Invoke(startedOp);
        return scope;
    }

    public void Clear()
    {
        lock (_lock)
        {
            _history.Clear();
        }
    }

    internal void ReplaceOperation(VsOperationCall completedOp)
    {
        lock (_lock)
        {
            var index = _history.FindIndex(o => o.Id == completedOp.Id);
            if (index >= 0)
            {
                _history[index] = completedOp;
            }
        }
    }

    internal void NotifyOperationCompleted(VsOperationCall completedOp)
    {
        OperationCompleted?.Invoke(completedOp);
    }
}
