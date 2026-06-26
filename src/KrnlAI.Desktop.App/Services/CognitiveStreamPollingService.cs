using System.Net.Http;
using KrnlAI.Desktop.Core.Abstractions;
using CognitiveCycleEvent = KrnlAI.Desktop.Core.Abstractions.CognitiveCycleEvent;
using System.IO;

namespace KrnlAI.Desktop.App.Services;

public sealed class CognitiveStreamPollingService
{
    private readonly HttpClient _http;
    private CancellationTokenSource? _cts;
    private string? _cycleId;
    private readonly object _connectLock = new();

    public CognitiveStreamState State { get; private set; } = CognitiveStreamState.Disconnected;
    public List<CognitiveCycleEvent> Events { get; } = [];
    public event Action<CognitiveCycleEvent>? OnEvent;
    public event Action<CognitiveStreamState>? OnStateChanged;

    public CognitiveStreamPollingService(string baseUrl = "http://localhost:5235", HttpMessageHandler? handler = null)
    {
        _http = handler is null ? new HttpClient() : new HttpClient(handler);
        _http.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task ConnectAsync(string? cycleId = null, CancellationToken ct = default)
    {
        lock (_connectLock)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _cycleId = cycleId;
        }

        State = CognitiveStreamState.Connecting;
        OnStateChanged?.Invoke(State);

        State = CognitiveStreamState.Connected;
        OnStateChanged?.Invoke(State);

        var token = _cts.Token;
        _ = Task.Run(() => ReadSseAsync(token), token);
    }

    public void Disconnect()
    {
        lock (_connectLock)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _cycleId = null;
        }
        State = CognitiveStreamState.Disconnected;
        OnStateChanged?.Invoke(State);
    }

    private async Task ReadSseAsync(CancellationToken ct)
    {
        string? cycleId;
        lock (_connectLock) { cycleId = _cycleId; }

        if (cycleId == null) return;
        try
        {
            using var response = await _http.GetAsync(
                $"/api/cognitive/stream/{Uri.EscapeDataString(cycleId)}",
                HttpCompletionOption.ResponseHeadersRead,
                ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return;

            using var stream = (await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false));
            using var reader = new StreamReader(stream);
            var eventLines = new List<string>();

            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is null) break;
                if (line.Length == 0)
                {
                    TryPublishSseEvent(eventLines);
                    eventLines.Clear();
                    continue;
                }

                if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    eventLines.Add(line["data:".Length..].Trim());
            }

            TryPublishSseEvent(eventLines);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Core.Services.KrnlLogger.Write($"CognitiveStreamPollingService: {ex.Message}");
            State = CognitiveStreamState.Error;
            OnStateChanged?.Invoke(State);
        }
    }

    private void TryPublishSseEvent(List<string> eventLines)
    {
        if (eventLines.Count == 0) return;
        try
        {
            var json = string.Join(Environment.NewLine, eventLines);
            var evt = System.Text.Json.JsonSerializer.Deserialize<CognitiveCycleEvent>(json);
            if (evt == null) return;
            Events.Add(evt);
            OnEvent?.Invoke(evt);
        }
        catch (Exception ex)
        {
            Core.Services.KrnlLogger.Write($"CognitiveStreamPollingService: malformed event: {ex.Message}");
        }
    }
}
