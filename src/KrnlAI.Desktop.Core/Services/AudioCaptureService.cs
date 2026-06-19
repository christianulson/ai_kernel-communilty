using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.Core.Services;

public class AudioCaptureService(ILogger<AudioCaptureService> logger, int maxAudioBufferSize = 10 * 1024 * 1024) : IAudioCapture
{
    private NAudio.Wave.WaveInEvent? _waveIn;
    private readonly List<byte> _audioBuffer = [];
    private bool _isCapturing;
    private string? _selectedDeviceId;
    private int _sampleRate = 16000;
    private int _channels = 1;
    private int _bitsPerSample = 16;
    private readonly object _lock = new();
    private const int BufferSizeMs = 500;
    private readonly int _maxAudioBufferSize = Math.Max(1, maxAudioBufferSize);

    public event EventHandler<float>? VoiceLevelChanged;

    public bool IsCapturing => _isCapturing;

    public Task StartCaptureAsync(string? deviceId = null)
    {
        if (_isCapturing) return Task.CompletedTask;

        _selectedDeviceId = deviceId;

        lock (_lock)
        {
            _audioBuffer.Clear();
        }

        var deviceNumber = GetDeviceNumber(deviceId);

        _waveIn = new NAudio.Wave.WaveInEvent
        {
            WaveFormat = new NAudio.Wave.WaveFormat(_sampleRate, _bitsPerSample, _channels),
            DeviceNumber = deviceNumber,
            BufferMilliseconds = BufferSizeMs
        };

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;

        _waveIn.StartRecording();
        _isCapturing = true;

        logger.LogInformation("Audio capture started with device: {DeviceId}, sampleRate: {SampleRate}",
            deviceId ?? "default", _sampleRate);
        return Task.CompletedTask;
    }

    public Task StopCaptureAsync()
    {
        if (!_isCapturing || _waveIn == null) return Task.CompletedTask;

        _waveIn.StopRecording();
        _waveIn.Dispose();
        _waveIn = null;
        _isCapturing = false;

        lock (_lock)
        {
            _audioBuffer.Clear();
        }

        logger.LogInformation("Audio capture stopped");
        return Task.CompletedTask;
    }

    public Task<byte[]> StopCaptureAndGetAudioAsync()
    {
        if (!_isCapturing || _waveIn == null)
            return Task.FromResult(Array.Empty<byte>());

        _waveIn.StopRecording();
        _waveIn.Dispose();
        _waveIn = null;
        _isCapturing = false;

        byte[] result;
        lock (_lock)
        {
            result = [.. _audioBuffer];
            _audioBuffer.Clear();
        }

        logger.LogInformation("Audio capture stopped, captured {Length} bytes", result.Length);
        return Task.FromResult(result);
    }

    private void OnDataAvailable(object? sender, NAudio.Wave.WaveInEventArgs e)
    {
        float maxLevel = 0;
        for (var i = 0; i < e.BytesRecorded; i += 2)
        {
            if (i + 1 >= e.BytesRecorded) break;
            var sample = BitConverter.ToInt16(e.Buffer, i);
            var level = Math.Abs(sample / 32768f);
            if (level > maxLevel) maxLevel = level;
        }

        VoiceLevelChanged?.Invoke(this, maxLevel);

        lock (_lock)
        {
            _audioBuffer.AddRange(e.Buffer.Take(e.BytesRecorded));
            var excess = _audioBuffer.Count - _maxAudioBufferSize;
            if (excess > 0)
            {
                _audioBuffer.RemoveRange(0, excess);
            }
        }
    }

    private void OnRecordingStopped(object? sender, NAudio.Wave.StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            logger.LogError(e.Exception, "Recording stopped with error");
        }
    }

    public IReadOnlyList<MediaDevice> GetAvailableDevices()
    {
        var devices = new List<MediaDevice>();

        try
        {
            for (var i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++)
            {
                var capabilities = NAudio.Wave.WaveIn.GetCapabilities(i);
                devices.Add(new MediaDevice(i.ToString(), capabilities.ProductName, MediaDeviceType.AudioInput));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error enumerating audio devices");
        }

        if (devices.Count == 0)
        {
            devices.Add(new MediaDevice("0", "Default Microphone", MediaDeviceType.AudioInput));
        }

        return devices;
    }

    private int GetDeviceNumber(string? deviceId)
    {
        if (string.IsNullOrEmpty(deviceId)) return 0;
        return int.TryParse(deviceId, out var number) ? number : 0;
    }

    public void Dispose()
    {
        if (_isCapturing)
        {
            _ = StopCaptureAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    logger.LogError(t.Exception, "Error stopping audio capture on dispose");
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
