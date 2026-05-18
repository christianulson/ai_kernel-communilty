using System.Windows;
using System.Windows.Media.Imaging;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Abstractions;
using System.Windows.Media;

namespace KrnlAI.Desktop.App;

public partial class MainWindow : Window
{
    private readonly ServiceLocator _services;
    private System.Threading.Timer? _videoTimer;
    private bool _isVideoRunning;
    public event EventHandler? LogoutRequested;

    private readonly MainViewModel? _vm;
    private EventHandler? _logoutHandler;

    public MainWindow()
    {
        InitializeComponent();

        _services = ServiceLocator.Instance;
        _vm = DataContext as MainViewModel;

        if (_vm != null)
        {
            _logoutHandler = (s, e) => LogoutRequested?.Invoke(this, EventArgs.Empty);
            _vm.LogoutRequested += _logoutHandler;
        }

        Closing += MainWindow_Closing;
    }

    public void SetAlwaysOnTop(bool value)
    {
        Topmost = value;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _videoTimer?.Dispose();
        StopVideoPreview();
        if (_vm != null)
        {
            if (_logoutHandler != null) _vm.LogoutRequested -= _logoutHandler;
            _vm.Cleanup();
        }
    }

    public void StartVideoPreview()
    {
        if (_isVideoRunning) return;
        _isVideoRunning = true;

        _videoTimer = new System.Threading.Timer(_ =>
        {
            try
            {
                var capture = _services.VideoCapture;
                if (capture == null) return;

                var devices = capture.GetAvailableDevices();
                if (devices.Count == 0) return;

                // Subscribe to frame capture for live preview
                void handler(object? s, VideoCaptureEventArgs args)
                {
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var bitmap = ConvertToBitmapSource(args.ImageData, args.Width, args.Height);
                            // Camera preview frame received
                        }
                        catch (Exception ex) { System.Diagnostics.Trace.WriteLine($"Video frame error: {ex.Message}"); }
                    });
                    capture.FrameCaptured -= handler;
                }
                capture.FrameCaptured += handler;
                _ = capture.StartCaptureAsync(devices[0].Id);
            }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine($"Video init error: {ex.Message}"); }
        }, null, 0, 100);
    }

    public void StopVideoPreview()
    {
        _isVideoRunning = false;
        _videoTimer?.Dispose();
        _videoTimer = null;
        _ = _services.VideoCapture.StopCaptureAsync();
    }

    public void ToggleAlwaysOnTop()
    {
        Topmost = !Topmost;
    }

    private static BitmapSource? ConvertToBitmapSource(byte[] frameData, int width, int height)
    {
        try
        {
            var stride = width * 3;
            var bitmap = BitmapSource.Create(width, height, 96, 96,
                PixelFormats.Bgr24, null, frameData, stride);
            return bitmap;
        }
        catch (ArgumentException ex)
        {
            System.Diagnostics.Trace.WriteLine($"Failed to create bitmap: {ex.Message}");
            return null;
        }
    }
}
