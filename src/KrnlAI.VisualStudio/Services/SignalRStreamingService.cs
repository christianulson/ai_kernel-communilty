using Microsoft.AspNetCore.SignalR.Client;

namespace KrnlAI.VisualStudio.Services;

public sealed class SignalRStreamingService : ISignalRStreamingService, IAsyncDisposable
{
    private HubConnection? _connection;
    private ConnectionState _state = ConnectionState.Disconnected;
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private CancellationTokenSource? _reconnectCts;
    private string? _lastHubUrl;

    public ConnectionState State
    {
        get => _state;
        private set
        {
            if (_state == value) return;
            _state = value;
            StateChanged?.Invoke(value);
        }
    }

    public event Action<string>? TokenReceived;
    public event Action<string>? ArtifactReceived;
    public event Action<string>? ErrorReceived;
    public event Action? StreamCompleted;
    public event Action<ConnectionState>? StateChanged;

    public async Task ConnectAsync(string hubUrl, CancellationToken ct = default)
    {
        await _connectLock.WaitAsync(ct);
        try
        {
            _lastHubUrl = hubUrl;
            State = ConnectionState.Connecting;

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16) })
                .Build();

#pragma warning disable VSTHRD001
            _connection.On<string>("token", token =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() => TokenReceived?.Invoke(token));
            });

            _connection.On<string>("artifact", artifact =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() => ArtifactReceived?.Invoke(artifact));
            });

            _connection.On<string>("error", error =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() => ErrorReceived?.Invoke(error));
            });

            _connection.On("completed", () =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() => StreamCompleted?.Invoke());
            });
#pragma warning restore VSTHRD001

            _connection.Reconnecting += _ =>
            {
                State = ConnectionState.Connecting;
                return Task.CompletedTask;
            };

            _connection.Reconnected += _ =>
            {
                State = ConnectionState.Connected;
                return Task.CompletedTask;
            };

            _connection.Closed += async _ =>
            {
                State = ConnectionState.Disconnected;
                await TryReconnectAsync();
            };

            await _connection.StartAsync(ct);
            State = ConnectionState.Connected;
        }
        catch
        {
            State = ConnectionState.Failed;
            throw;
        }
        finally
        {
            _connectLock.Release();
        }
    }

    public async Task DisconnectAsync()
    {
        _reconnectCts?.Cancel();
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
        State = ConnectionState.Disconnected;
    }

    public async Task StartAgentStreamAsync(string goal, string sessionId, CancellationToken ct = default)
    {
        if (_connection?.State != HubConnectionState.Connected)
            throw new InvalidOperationException("Not connected to SignalR hub.");

        await _connection.InvokeAsync("StartAgentStream", goal, sessionId, ct);
    }

    private async Task TryReconnectAsync()
    {
        if (_reconnectCts?.IsCancellationRequested == true) return;
        _reconnectCts?.Cancel();
        _reconnectCts = new CancellationTokenSource();
        var ct = _reconnectCts.Token;

        var delays = new[] { 1, 2, 4, 8, 16, 30 };
        for (int i = 0; i < delays.Length; i++)
        {
            if (ct.IsCancellationRequested) return;
            await Task.Delay(TimeSpan.FromSeconds(delays[i]), ct);
            if (ct.IsCancellationRequested) return;

            try
            {
                await ConnectAsync(_lastHubUrl ?? "", ct);
                return;
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"[KrnlAI] Reconnect attempt {i + 1} failed");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _connectLock.Dispose();
        _reconnectCts?.Dispose();
    }
}
