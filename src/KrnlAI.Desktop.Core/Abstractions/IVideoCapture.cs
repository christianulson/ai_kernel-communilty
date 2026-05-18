using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface IVideoCapture : IDisposable
{
    event EventHandler<VideoCaptureEventArgs>? FrameCaptured;
    Task StartCaptureAsync(string? deviceId = null);
    Task StopCaptureAsync();
    bool IsCapturing { get; }
    IReadOnlyList<MediaDevice> GetAvailableDevices();
}

public record VideoCaptureEventArgs(
    byte[] ImageData,
    int Width,
    int Height,
    TimeSpan Timestamp
);