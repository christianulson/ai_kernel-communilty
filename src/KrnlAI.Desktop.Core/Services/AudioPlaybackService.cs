using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.Core.Services;

public class AudioPlaybackService(ILogger<AudioPlaybackService> logger) : IAudioPlayback
{
    private NAudio.Wave.WaveOutEvent? _waveOut;
    private NAudio.Wave.WaveFileReader? _waveReader;
    private MemoryStream? _audioStream;
    private string? _selectedDeviceId;
    private float _volume = 1.0f;
    private bool _isPlaying;

    public event EventHandler? PlaybackStarted;
    public event EventHandler? PlaybackStopped;

    public bool IsPlaying => _isPlaying;

    public async Task PlayAsync(byte[] audioData, CancellationToken cancellationToken = default)
    {
        Stop();

        try
        {
            // Keep the MemoryStream alive as long as _waveReader references it
            _audioStream?.Dispose();
            _audioStream = new MemoryStream(audioData);
            _waveReader = new NAudio.Wave.WaveFileReader(_audioStream);

            var deviceNumber = GetDeviceNumber(_selectedDeviceId);

            _waveOut = new NAudio.Wave.WaveOutEvent
            {
                DeviceNumber = deviceNumber
            };

            _waveOut.Init(_waveReader);
            _waveOut.Volume = _volume;

            _isPlaying = true;
            PlaybackStarted?.Invoke(this, EventArgs.Empty);

            logger.LogInformation("Starting audio playback");

            await Task.Run(() =>
            {
                _waveOut?.Play();
                while (_waveOut?.PlaybackState == NAudio.Wave.PlaybackState.Playing && !cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(100);
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Audio playback cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error playing audio");
        }
        finally
        {
            Stop();
        }
    }

    public void Stop()
    {
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;

        _waveReader?.Dispose();
        _waveReader = null;

        _audioStream?.Dispose();
        _audioStream = null;

        if (_isPlaying)
        {
            _isPlaying = false;
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }
    }

    public IReadOnlyList<MediaDevice> GetAvailableDevices()
    {
        var devices = new List<MediaDevice>();

        for (var i = 0; i < NAudio.Wave.WaveOut.DeviceCount; i++)
        {
            var capabilities = NAudio.Wave.WaveOut.GetCapabilities(i);
            devices.Add(new MediaDevice(i.ToString(), capabilities.ProductName, MediaDeviceType.AudioOutput));
        }

        if (devices.Count == 0)
        {
            devices.Add(new MediaDevice("0", "Default Speaker", MediaDeviceType.AudioOutput));
        }

        return devices;
    }

    public void SetDevice(string? deviceId)
    {
        _selectedDeviceId = deviceId;
        logger.LogInformation("Audio output device set to: {DeviceId}", deviceId ?? "default");
    }

    public void SetVolume(float volume)
    {
        _volume = Math.Clamp(volume, 0f, 1f);
        if (_waveOut != null)
        {
            _waveOut.Volume = _volume;
        }
    }

    private int GetDeviceNumber(string? deviceId)
    {
        if (string.IsNullOrEmpty(deviceId)) return 0;
        return int.TryParse(deviceId, out var number) ? number : 0;
    }

    public void Dispose()
    {
        Stop();
    }
}