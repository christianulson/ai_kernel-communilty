using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.Core.Services;

public interface IWebRtcService
{
    event EventHandler<WebRtcEventArgs>? StateChanged;

    bool IsConnected { get; }
    string LocalPeerId { get; }
    string? RemotePeerId { get; }
    Task<bool> InitializeAsync(string signalingUrl, string stunServer, string? turnServer = null);
    Task<bool> CreateAndSendOfferAsync(string targetPeerId);
    Task<bool> ConnectToPeerAsync(string peerId);
    Task DisconnectAsync();
    Task SendAudioFrameAsync(byte[] pcmData);
    Task SendVideoFrameAsync(byte[] frameData);
}

public class WebRtcEventArgs : EventArgs
{
    public WebRtcState State { get; init; }
    public string? Message { get; init; }
    public string? PeerId { get; init; }
}

public enum WebRtcState
{
    Disconnected,
    Connecting,
    Connected,
    Failed,
    Closed
}

public sealed class WebRtcService : IWebRtcService, IDisposable
{
    public event EventHandler<WebRtcEventArgs>? StateChanged;

    private readonly ILogger<WebRtcService> _logger;
    private ClientWebSocket? _ws;
    private string _localPeerId = "";
    private string? _remotePeerId;
    private string _signalingUrl = "";
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private bool _disposed;

    public bool IsConnected => _ws?.State == WebSocketState.Open;
    public string LocalPeerId => _localPeerId;
    public string? RemotePeerId => _remotePeerId;

    public WebRtcService(ILogger<WebRtcService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> InitializeAsync(string signalingUrl, string stunServer, string? turnServer = null)
    {
        try
        {
            _signalingUrl = signalingUrl;
            _localPeerId = Guid.NewGuid().ToString("N")[..16];
            _cts = new CancellationTokenSource();
            OnStateChanged(WebRtcState.Disconnected, "WebRTC inicializado");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize WebRTC");
            OnStateChanged(WebRtcState.Failed, ex.Message);
            return false;
        }
    }

    public async Task<bool> CreateAndSendOfferAsync(string targetPeerId)
    {
        try
        {
            _remotePeerId = targetPeerId;
            await ConnectWebSocketAsync();
            await SendJsonAsync(new { type = "offer", source = _localPeerId, target = targetPeerId });
            OnStateChanged(WebRtcState.Connecting, "Offer enviado");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send offer");
            OnStateChanged(WebRtcState.Failed, ex.Message);
            return false;
        }
    }

    public async Task<bool> ConnectToPeerAsync(string peerId)
    {
        try
        {
            _remotePeerId = peerId;
            await ConnectWebSocketAsync();
            await SendJsonAsync(new { type = "join", peerId = _localPeerId, target = peerId });
            OnStateChanged(WebRtcState.Connecting, $"Conectando a {peerId}...");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to peer");
            OnStateChanged(WebRtcState.Failed, ex.Message);
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        _cts?.Cancel();
        try
        {
            await SendJsonAsync(new { type = "leave", peerId = _localPeerId });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to notify signaling server about peer disconnect");
        }
        if (_ws?.State == WebSocketState.Open)
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        OnStateChanged(WebRtcState.Closed, "Desconectado");
    }

    public async Task SendAudioFrameAsync(byte[] pcmData)
    {
        if (!IsConnected) return;
        try { await SendJsonAsync(new { type = "audio", data = Convert.ToBase64String(pcmData) }); }
        catch (Exception ex) { _logger.LogDebug(ex, "Failed to send audio"); }
    }

    public async Task SendVideoFrameAsync(byte[] frameData)
    {
        if (!IsConnected) return;
        try { await SendJsonAsync(new { type = "video", data = Convert.ToBase64String(frameData) }); }
        catch (Exception ex) { _logger.LogDebug(ex, "Failed to send video"); }
    }

    private async Task ConnectWebSocketAsync()
    {
        if (_ws?.State == WebSocketState.Open) return;
        _ws?.Dispose();
        _ws = new ClientWebSocket();
        await _ws.ConnectAsync(new Uri(_signalingUrl), _cts?.Token ?? CancellationToken.None);
        _receiveTask = ReceiveLoopAsync();
    }

    private async Task ReceiveLoopAsync()
    {
        var buffer = new byte[65536];
        try
        {
            while (_ws?.State == WebSocketState.Open && !_cts?.IsCancellationRequested == true)
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts?.Token ?? CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) break;
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                ProcessMessage(json);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation during shutdown
        }
        catch (Exception ex) { _logger.LogError(ex, "WebSocket receive error"); }
    }

    private void ProcessMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var type = doc.RootElement.GetProperty("type").GetString();
            switch (type)
            {
                case "connected":
                    _remotePeerId = doc.RootElement.TryGetProperty("peerId", out var pid) ? pid.GetString() : _remotePeerId;
                    OnStateChanged(WebRtcState.Connected, "Conectado", _remotePeerId);
                    break;
                case "offer":
                case "answer":
                    OnStateChanged(WebRtcState.Connected, "Sinalizacao recebida");
                    break;
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to process message"); }
    }

    private Task SendJsonAsync(object msg)
    {
        if (_ws?.State != WebSocketState.Open) return Task.CompletedTask;
        var json = JsonSerializer.Serialize(msg);
        return _ws.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, _cts?.Token ?? CancellationToken.None);
    }

    private void OnStateChanged(WebRtcState state, string? message, string? peerId = null)
    {
        StateChanged?.Invoke(this, new WebRtcEventArgs { State = state, Message = message, PeerId = peerId });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts?.Cancel();
        _ws?.Dispose();
        _cts?.Dispose();
    }
}
