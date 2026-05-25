using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.Core.Services;

public class ListeningService : IListeningService
{
    private readonly IAudioCapture _audioCapture;
    private readonly IKernelAgentClient _kernelAgentClient;
    private readonly IKernelSpeechClient _kernelSpeechClient;
    private readonly IAudioPlayback _audioPlayback;
    private readonly ILogger<ListeningService> _logger;

    private float _threshold = 0.01f;
    private int _silenceDurationMs = 1500;
    private bool _isListening;
    private const int MaxAudioBufferSize = 10 * 1024 * 1024; // 10 MB max
    private DateTime _lastSpeechTime = DateTime.MinValue;
    private bool _wasSpeaking;
    private readonly List<byte> _audioBuffer = new();
    private CancellationTokenSource? _cts;
    private Task? _processingTask = null;
    private readonly object _lock = new();
    private readonly SemaphoreSlim _processingGate = new(1, 1);

    public event EventHandler<float>? VoiceLevelChanged;
    public event EventHandler<ListeningEventArgs>? SpeechDetected;
    public event EventHandler<string>? ResponseReceived;

    public bool IsListening => _isListening;

    private readonly bool _isLocalMode;

    public ListeningService(
        IAudioCapture audioCapture,
        IKernelAgentClient kernelAgentClient,
        IKernelSpeechClient kernelSpeechClient,
        IAudioPlayback audioPlayback,
        ILogger<ListeningService> logger,
        bool isLocalMode = false)
    {
        _audioCapture = audioCapture;
        _kernelAgentClient = kernelAgentClient;
        _kernelSpeechClient = kernelSpeechClient;
        _audioPlayback = audioPlayback;
        _logger = logger;
        _isLocalMode = isLocalMode;

        _audioCapture.VoiceLevelChanged += OnVoiceLevelChanged;
    }

    public async Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        if (_isListening) return;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lock (_lock)
        {
            _audioBuffer.Clear();
            _lastSpeechTime = DateTime.MinValue;
            _wasSpeaking = false;
        }

        try
        {
            await _audioCapture.StartCaptureAsync();
            _isListening = _audioCapture.IsCapturing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start audio capture");
            _isListening = false;
            return;
        }

        _logger.LogInformation("Continuous listening started with threshold {Threshold}, capturing={IsCapturing}", _threshold, _isListening);
    }

    public async Task StopListeningAsync()
    {
        if (!_isListening) return;

        _cts?.Cancel();

        if (_processingTask != null)
        {
            try
            {
                await _processingTask;
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation during shutdown
            }
        }

        await _audioCapture.StopCaptureAsync();

        _isListening = false;
        lock (_lock)
        {
            _audioBuffer.Clear();
        }

        _logger.LogInformation("Continuous listening stopped");
    }

    private void OnVoiceLevelChanged(object? sender, float level)
    {
        VoiceLevelChanged?.Invoke(this, level);

        if (!_isListening) return;

        bool isSpeaking = level > _threshold;

        if (isSpeaking)
        {
            _lastSpeechTime = DateTime.UtcNow;
            _wasSpeaking = true;
        }
        else if (_wasSpeaking && (DateTime.UtcNow - _lastSpeechTime).TotalMilliseconds > _silenceDurationMs)
        {
            _wasSpeaking = false;

            if (_processingGate.Wait(0))
            {
                _ = ProcessSpeechInternalAsync();
            }
        }
    }

    private async Task ProcessSpeechInternalAsync()
    {
        try
        {
            _processingTask = ProcessSpeechAsync();
            await _processingTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in voice level handler");
        }
        finally
        {
            _processingTask = null;
            _processingGate.Release();
        }
    }

    private async Task ProcessSpeechAsync()
    {
        if (_isLocalMode)
        {
            _logger.LogInformation("Speech processing skipped (Local mode)");
            return;
        }

        byte[] audioData;
        lock (_lock)
        {
            if (_audioBuffer.Count == 0) return;
            audioData = _audioBuffer.ToArray();
            _audioBuffer.Clear();
        }

        var duration = TimeSpan.FromSeconds((double)audioData.Length / 16000.0);
        _logger.LogInformation("Processing speech, audio size: {Size}, duration: {Duration}s", audioData.Length, duration.TotalSeconds);

        SpeechDetected?.Invoke(this, new ListeningEventArgs(audioData, duration));

        try
        {
            string? transcribedText = null;

            var transcription = await _kernelSpeechClient.TranscribeAudioAsync(audioData, _cts?.Token ?? CancellationToken.None);
            if (!string.IsNullOrEmpty(transcription))
            {
                transcribedText = transcription;
                _logger.LogDebug("Transcription completed, length: {Len} chars", transcription.Length);
            }

            var promptText = !string.IsNullOrEmpty(transcribedText)
                ? transcribedText
                : $"Audio captured: {audioData.Length} bytes";

            var response = await _kernelAgentClient.RunAgentAsync(new AgentRunRequest(
                promptText,
                Mode: "gateway"
            ), _cts?.Token ?? CancellationToken.None);

            if (!string.IsNullOrEmpty(response.Narration))
            {
                _logger.LogDebug("Received response, length: {Len} chars", response.Narration.Length);

                ResponseReceived?.Invoke(this, response.Narration);

                var ttsAudio = await _kernelSpeechClient.GenerateSpeechAsync(response.Narration, "pt-BR");
                if (ttsAudio.Length > 0)
                {
                    await _audioPlayback.PlayAsync(ttsAudio, _cts?.Token ?? CancellationToken.None);
                }
            }
            else if (!string.IsNullOrEmpty(response.Error))
            {
                _logger.LogWarning("Error in response: {Error}", response.Error);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Speech processing cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing speech");
        }
    }

    public void SetThreshold(float threshold)
    {
        _threshold = Math.Clamp(threshold, 0.001f, 1.0f);
        _logger.LogInformation("Voice detection threshold set to {Threshold}", _threshold);
    }

    public void SetSilenceDuration(int milliseconds)
    {
        _silenceDurationMs = Math.Clamp(milliseconds, 500, 10000);
        _logger.LogInformation("Silence duration set to {Duration}ms", _silenceDurationMs);
    }

    public void Dispose()
    {
        if (_isListening)
        {
            _ = StopListeningAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    _logger.LogError(t.Exception, "Error stopping listening on dispose");
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
        _processingGate.Dispose();
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
