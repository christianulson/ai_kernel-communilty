using KrnlAI.Desktop.Core.Abstractions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KrnlAI.Desktop.Core.Services;

public sealed class SensoryIngestClient : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly string _hubUrl;
    private readonly ILogger<SensoryIngestClient>? _logger;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public SensoryIngestClient(string hubUrl = "http://localhost:5000/hubs/sensory-ingestion",
        ILogger<SensoryIngestClient>? logger = null)
    {
        _hubUrl = hubUrl;
        _logger = logger;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.Reconnecting += _ =>
        {
            _logger?.LogWarning("SensoryIngestClient reconnecting...");
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            _logger?.LogInformation("SensoryIngestClient reconnected");
            return Task.CompletedTask;
        };

        await _connection.StartAsync(ct);
        _logger?.LogInformation("SensoryIngestClient connected to {Hub}", _hubUrl);
    }

    public async Task SendAudioFrameAsync(byte[] wavData, double intensity, string? transcription = null)
    {
        if (_connection?.State != HubConnectionState.Connected) return;
        try
        {
            await _connection.InvokeAsync("IngestAudioChunk",
                wavData, "wav", intensity, transcription);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to send audio frame");
        }
    }

    public async Task SendVideoFrameAsync(byte[] jpegData, double intensity, string? description = null)
    {
        if (_connection?.State != HubConnectionState.Connected) return;
        try
        {
            await _connection.InvokeAsync("IngestVideoFrame",
                jpegData, intensity, description);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to send video frame");
        }
    }

    public async Task SendBatchAsync(IReadOnlyList<byte[]> frames, IReadOnlyList<(byte[] Data, string Format)>? audioClips)
    {
        if (_connection?.State != HubConnectionState.Connected) return;
        try
        {
            var frameData = frames.Select(f => new { Data = f, Intensity = 0.5, Description = (string?)null }).ToArray();
            var clips = audioClips?.Select(a => new { a.Data, a.Format, Intensity = 0.5, Transcription = (string?)null }).ToArray();

            await _connection.InvokeAsync("IngestBatch", new
            {
                Frames = frameData,
                AudioClips = clips
            });
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to send batch");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
