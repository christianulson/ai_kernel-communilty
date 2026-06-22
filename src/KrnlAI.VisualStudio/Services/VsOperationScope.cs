using System.Diagnostics;

namespace KrnlAI.VisualStudio.Services;

public sealed class VsOperationScope : IDisposable
{
    private readonly VsOperationTracker _tracker;
    private readonly string _id;
    private readonly string _name;
    private readonly string? _arguments;
    private readonly DateTime _startedAt;
    private readonly Stopwatch _stopwatch;
    private readonly bool _isRoot;
    private bool _disposed;
    private bool _hasError;
    private string? _result;
    private string? _error;
    private VsOperationState _state = VsOperationState.Running;
    private readonly List<VsOperationScope> _childScopes = [];

    internal VsOperationScope(
        VsOperationTracker tracker,
        string id,
        string name,
        string? arguments,
        bool isRoot = true)
    {
        _tracker = tracker;
        _id = id;
        _name = name;
        _arguments = arguments;
        _startedAt = DateTime.UtcNow;
        _stopwatch = Stopwatch.StartNew();
        _isRoot = isRoot;
    }

    public void SetResult(string result)
    {
        if (_disposed) return;
        _result = result;
    }

    public void SetError(string error)
    {
        if (_disposed) return;
        _hasError = true;
        _error = error;
    }

    public VsOperationScope StartChild(string name, string? arguments = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(VsOperationScope), "Cannot start child on a disposed operation scope.");

        var childId = $"{_id}.{_childScopes.Count + 1}";
        var child = new VsOperationScope(_tracker, childId, name, arguments, isRoot: false);
        _childScopes.Add(child);
        return child;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stopwatch.Stop();

        // Auto-dispose any children that weren't explicitly disposed
        foreach (var child in _childScopes)
        {
            if (!child._disposed)
            {
                child._disposed = true;
                child._stopwatch.Stop();
                child._state = child._hasError ? VsOperationState.Failed : VsOperationState.Completed;
            }
        }

        if (_hasError)
        {
            _state = VsOperationState.Failed;
        }
        else
        {
            _state = VsOperationState.Completed;
        }

        var completedOp = ToOperationCall();
        if (_isRoot)
        {
            _tracker.ReplaceOperation(completedOp);
            _tracker.NotifyOperationCompleted(completedOp);
        }
    }

    internal VsOperationCall ToOperationCall()
    {
        IReadOnlyList<VsOperationCall>? children = null;
        if (_childScopes.Count > 0)
        {
            children = _childScopes.Select(c => c.ToOperationCall()).ToList();
        }

        return new VsOperationCall(
            Id: _id,
            Name: _name,
            Arguments: _arguments,
            State: _state,
            Result: _result,
            Error: _error,
            ElapsedMs: _stopwatch.ElapsedMilliseconds,
            StartedAt: _startedAt,
            Children: children
        );
    }
}
