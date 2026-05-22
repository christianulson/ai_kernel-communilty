using System.Net.Http;
using System.Net.Http.Json;
using KrnlAI.Desktop.Core.Abstractions;
using CognitiveCycleEvent = KrnlAI.Desktop.Core.Abstractions.CognitiveCycleEvent;
using System.IO;

namespace KrnlAI.Desktop.App.Services;

public sealed class CognitiveStreamPollingService
{
    private readonly HttpClient _http;
    private CancellationTokenSource? _cts;
    private string? _cycleId;

    public CognitiveStreamState State { get; private set; } = CognitiveStreamState.Disconnected;
    public List<CognitiveCycleEvent> Events { get; } = new();
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
        _cycleId = cycleId;
        State = CognitiveStreamState.Connecting;
        OnStateChanged?.Invoke(State);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        State = CognitiveStreamState.Connected;
        OnStateChanged?.Invoke(State);

        _ = ReadSseAsync(_cts.Token);
    }

    public void Disconnect()
    {
        _cts?.Cancel();
        _cts = null;
        State = CognitiveStreamState.Disconnected;
        OnStateChanged?.Invoke(State);
    }

    private async Task ReadSseAsync(CancellationToken ct)
    {
        if (_cycleId == null) return;
        try
        {
            using var response = await _http.GetAsync(
                $"/api/cognitive/stream/{Uri.EscapeDataString(_cycleId)}",
                HttpCompletionOption.ResponseHeadersRead,
                ct);
            if (!response.IsSuccessStatusCode) return;

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);
            var eventLines = new List<string>();

            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
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
        catch
        {
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
        catch
        {
            // Ignore malformed best-effort stream events.
        }
    }
}
