using System.Buffers;
using System.Runtime.InteropServices;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;
using OpenCvSharp;

namespace KrnlAI.Desktop.Core.Services;

public class VideoCaptureService(ILogger<VideoCaptureService> logger) : IVideoCapture
{
    private VideoCapture? _capture;
    private bool _isCapturing;
    private string? _selectedDeviceId;
    private CancellationTokenSource? _cts;
    private Task? _captureTask;

    public event EventHandler<VideoCaptureEventArgs>? FrameCaptured;

    public bool IsCapturing => _isCapturing;

    public Task StartCaptureAsync(string? deviceId = null)
    {
        if (_isCapturing) return Task.CompletedTask;

        _selectedDeviceId = deviceId;
        _cts = new CancellationTokenSource();

        var deviceIndex = string.IsNullOrEmpty(deviceId) ? 0 : int.TryParse(deviceId, out var idx) ? idx : 0;

        try
        {
            _capture = new VideoCapture(deviceIndex);
            if (!_capture.IsOpened())
            {
                logger.LogError("Failed to open video capture device {DeviceIndex}", deviceIndex);
                return Task.CompletedTask;
            }

            _capture.Set(VideoCaptureProperties.FrameWidth, 1280);
            _capture.Set(VideoCaptureProperties.FrameHeight, 720);

            _isCapturing = true;
            _captureTask = Task.Run(() => CaptureLoop(_cts.Token));

            logger.LogInformation("Video capture started, resolution: {Width}x{Height}",
                _capture.Get(VideoCaptureProperties.FrameWidth),
                _capture.Get(VideoCaptureProperties.FrameHeight));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start video capture");
        }

        return Task.CompletedTask;
    }

    private async Task CaptureLoop(CancellationToken token)
    {
        using var frame = new Mat();
        var pool = ArrayPool<byte>.Shared;

        while (!token.IsCancellationRequested && _capture?.IsOpened() == true)
        {
            try
            {
                if (_capture.Read(frame) && !frame.Empty() && frame.Channels() >= 3)
                {
                    var width = frame.Width;
                    var height = frame.Height;
                    var channels = frame.Channels();
                    var length = width * height * channels;

                    var imageData = pool.Rent(length);
                    Marshal.Copy(frame.Data, imageData, 0, length);

                    var trimmed = imageData.AsSpan(0, length).ToArray();
                    pool.Return(imageData);

                    FrameCaptured?.Invoke(this, new VideoCaptureEventArgs(
                        trimmed,
                        width,
                        height,
                        DateTime.Now.TimeOfDay
                    ));
                }

                await Task.Delay(33, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error capturing frame");
            }
        }
    }

    public async Task StopCaptureAsync()
    {
        if (!_isCapturing) return;

        _cts?.Cancel();

        var captureTask = _captureTask;
        if (captureTask != null)
        {
            try
            {
                await captureTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _captureTask = null;

        _capture?.Release();
        _capture?.Dispose();
        _capture = null;
        _isCapturing = false;

        logger.LogInformation("Video capture stopped");
    }

    public IReadOnlyList<MediaDevice> GetAvailableDevices()
    {
        var devices = new List<MediaDevice>();

        // Test up to 20 camera indices to support systems with many cameras
        for (var i = 0; i < 20; i++)
        {
            try
            {
                using var capture = new VideoCapture(i);
                if (capture.IsOpened())
                {
                    devices.Add(new MediaDevice(i.ToString(), $"Camera {i + 1}", MediaDeviceType.VideoInput));
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to probe video capture device {DeviceIndex}", i);
            }
        }

        if (devices.Count == 0)
        {
            devices.Add(new MediaDevice("0", "Default Camera", MediaDeviceType.VideoInput));
        }

        return devices;
    }

    public void Dispose()
    {
        if (_isCapturing)
        {
            _cts?.Cancel();
            _capture?.Release();
            _capture?.Dispose();
            _capture = null;
            _isCapturing = false;
        }
    }
}
