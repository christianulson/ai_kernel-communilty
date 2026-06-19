using System.Windows;
using System.Windows.Controls;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class TerminalControl : UserControl
{
    private TerminalViewModel? _vm;

    public TerminalControl()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _vm = DataContext as TerminalViewModel;
            if (_vm != null)
            {
                var mainVm = Window.GetWindow(this)?.DataContext as MainViewModel;
                var baseUrl = mainVm?.ApiEndpoint ?? "http://localhost:5235";
                var hubUrl = baseUrl.TrimEnd('/') + "/hubs/terminal";
                _ = _vm.ConnectAsync(hubUrl);
            }
        }
        catch (Exception ex)
        {
            KrnlLogger.Write($"TerminalControl.Loaded: {ex.Message}");
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null)
        {
            _ = _vm.DisconnectAsync();
        }
    }
}
