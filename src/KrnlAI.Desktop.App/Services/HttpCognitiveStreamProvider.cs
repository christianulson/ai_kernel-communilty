using System.Net.Http;
using System.Net.Http.Json;
using KrnlAI.Desktop.Core.Abstractions;
using CognitiveCycleEvent = KrnlAI.Desktop.Core.Abstractions.CognitiveCycleEvent;

namespace KrnlAI.Desktop.App.Services;

public sealed class HttpCognitiveStreamProvider : ICognitiveStreamProvider
{
    private readonly HttpClient _http;
    private CancellationTokenSource? _cts;
    private string? _cycleId;

    public CognitiveStreamState State { get; private set; } = CognitiveStreamState.Disconnected;
    public List<CognitiveCycleEvent> Events { get; } = new();
    public event Action<CognitiveCycleEvent>? OnEvent;
    public event Action<CognitiveStreamState>? OnStateChanged;

    public HttpCognitiveStreamProvider(string baseUrl = "http://localhost:5235")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/')), Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task ConnectAsync(string? cycleId = null, CancellationToken ct = default)
    {
        _cycleId = cycleId;
        State = CognitiveStreamState.Connecting;
        OnStateChanged?.Invoke(State);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        State = CognitiveStreamState.Connected;
        OnStateChanged?.Invoke(State);

        _ = PollAsync(_cts.Token);
    }

    public void Disconnect()
    {
        _cts?.Cancel();
        _cts = null;
        State = CognitiveStreamState.Disconnected;
        OnStateChanged?.Invoke(State);
    }

    private async Task PollAsync(CancellationToken ct)
    {
        var lastEventCount = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, ct);
                if (_cycleId == null) continue;

                var response = await _http.GetAsync($"/api/cognitive/stream/{_cycleId}/events?since={lastEventCount}", ct);
                if (!response.IsSuccessStatusCode) continue;

                var events = await response.Content.ReadFromJsonAsync<List<CognitiveCycleEvent>>(ct);
                if (events == null || events.Count == 0) continue;

                foreach (var evt in events)
                {
                    Events.Add(evt);
                    OnEvent?.Invoke(evt);
                }
                lastEventCount += events.Count;
            }
            catch (OperationCanceledException) { break; }
            catch { /* polling best-effort */ }
        }
    }
}
