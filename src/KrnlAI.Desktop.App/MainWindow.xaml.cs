using System.Windows;
using System.Windows.Media.Imaging;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Services;
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

        if (SearchOverlayCtrl != null)
        {
            SearchOverlayCtrl.SearchRequested += query =>
            {
                var results = _vm?.SearchMessages(query) ?? new List<string>();
                SearchOverlayCtrl.SetResults(results);
            };
            SearchOverlayCtrl.DismissRequested += () =>
            {
                if (_vm != null) _vm.ShowSearch = false;
            };
        }

        Loaded += (_, _) =>
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && args[1] == "--minimized")
            {
                WindowState = System.Windows.WindowState.Minimized;
                Hide();
            }
        };
        Closing += MainWindow_Closing;
    }

    public void SetAlwaysOnTop(bool value)
    {
        Topmost = value;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
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

        var capture = _services.VideoCapture;
        if (capture == null) return;

        var devices = capture.GetAvailableDevices();
        if (devices.Count == 0) return;

        capture.FrameCaptured += OnFrameCaptured;
        _ = capture.StartCaptureAsync(devices[0].Id);
    }

    private void OnFrameCaptured(object? sender, VideoCaptureEventArgs args)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                _ = ConvertToBitmapSource(args.ImageData, args.Width, args.Height);
            }
            catch (Exception ex) { KrnlLogger.Write(ex); }
        });
    }

    public void StopVideoPreview()
    {
        _isVideoRunning = false;
        _videoTimer?.Dispose();
        _videoTimer = null;
        var capture = _services.VideoCapture;
        if (capture != null)
        {
            capture.FrameCaptured -= OnFrameCaptured;
            _ = capture.StopCaptureAsync();
        }
    }

    public void ToggleAlwaysOnTop()
    {
        Topmost = !Topmost;
    }

    private void OnCommandPaletteKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape && _vm != null) { _vm.ShowCommandPalette = false; e.Handled = true; }
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
            KrnlLogger.Write(ex);
            return null;
        }
    }
}
