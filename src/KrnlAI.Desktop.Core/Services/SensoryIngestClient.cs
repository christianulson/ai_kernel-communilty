using KrnlAI.Desktop.Core.Abstractions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.Core.Services;

public sealed class SensoryIngestClient(string? hubUrl = null,
    ILogger<SensoryIngestClient>? logger = null) : IAsyncDisposable
{
    private readonly string _hubUrl = hubUrl
        ?? ApiEndpointResolver.Resolve(null) + "/hubs/sensory-ingestion";
    private HubConnection? _connection;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.Reconnecting += _ =>
        {
            logger?.LogWarning("SensoryIngestClient reconnecting...");
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            logger?.LogInformation("SensoryIngestClient reconnected");
            return Task.CompletedTask;
        };

        await _connection.StartAsync(ct).ConfigureAwait(false);
        logger?.LogInformation("SensoryIngestClient connected to {Hub}", _hubUrl);
    }

    public async Task SendAudioFrameAsync(byte[] wavData, double intensity, string? transcription = null)
    {
        if (_connection?.State != HubConnectionState.Connected) return;
        try
        {
            await _connection.InvokeAsync("IngestAudioChunk",
                wavData, "wav", intensity, transcription).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to send audio frame");
        }
    }

    public async Task SendVideoFrameAsync(byte[] jpegData, double intensity, string? description = null)
    {
        if (_connection?.State != HubConnectionState.Connected) return;
        try
        {
            await _connection.InvokeAsync("IngestVideoFrame",
                jpegData, intensity, description).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to send video frame");
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
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to send batch");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
