using System.Collections.Concurrent;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Services;

public interface IOfflineService
{
    bool IsOffline { get; }
    event EventHandler<bool>? ConnectivityChanged;
    Task<bool> CacheCommandAsync(AgentRunRequest request);
    Task<List<AgentRunRequest>> GetCachedCommandsAsync();
    Task ClearCacheAsync();
}

public class OfflineService : IOfflineService
{
    private readonly ConcurrentQueue<AgentRunRequest> _commandQueue = new();
    private bool _isOffline;

    public bool IsOffline => _isOffline;
    public event EventHandler<bool>? ConnectivityChanged;

    public void SetOfflineStatus(bool offline)
    {
        if (_isOffline != offline)
        {
            _isOffline = offline;
            ConnectivityChanged?.Invoke(this, offline);
        }
    }

    public Task<bool> CacheCommandAsync(AgentRunRequest request)
    {
        _commandQueue.Enqueue(request);
        return Task.FromResult(true);
    }

    public Task<List<AgentRunRequest>> GetCachedCommandsAsync()
    {
        return Task.FromResult(_commandQueue.ToList());
    }

    public Task ClearCacheAsync()
    {
        while (_commandQueue.TryDequeue(out _)) { }
        return Task.CompletedTask;
    }
}
