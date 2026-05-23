using System.Windows;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Services;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace KrnlAI.Desktop.App;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private GlobalHotkeyService? _hotkeyService;
    private bool _isAlwaysOnTop;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        var settingsService = ServiceLocator.Instance.SettingsService;
        var settings = settingsService.LoadSettings();
        var isLocal = ServiceLocator.Instance.CurrentMode == RunMode.Local;

        if (!string.IsNullOrEmpty(settings.Theme))
        {
            ServiceLocator.Instance.ThemeService.SetTheme(settings.Theme);
        }

        if (!isLocal && string.IsNullOrEmpty(settings.AuthToken))
        {
            var loginWindow = new LoginWindow();
            var result = loginWindow.ShowDialog();

            if (result != true)
            {
                Shutdown();
                return;
            }

            var newSettings = settingsService.LoadSettings();
            newSettings = newSettings with { AuthToken = loginWindow.Token, RefreshToken = loginWindow.RefreshToken, Username = loginWindow.Username, IsAuthenticated = true };
            settingsService.SaveSettings(newSettings);
        }

        _mainWindow = new MainWindow();
        _mainWindow.LogoutRequested += OnLogoutRequested;
        
        if (!double.IsNaN(settings.WindowLeft) && !double.IsNaN(settings.WindowTop))
        {
            _mainWindow.Left = settings.WindowLeft;
            _mainWindow.Top = settings.WindowTop;
        }
        if (settings.WindowWidth > 0) _mainWindow.Width = settings.WindowWidth;
        if (settings.WindowHeight > 0) _mainWindow.Height = settings.WindowHeight;
        if (settings.WindowMaximized) _mainWindow.WindowState = WindowState.Maximized;
        
        _mainWindow.Closing += (s, e) =>
        {
            var currentSettings = settingsService.LoadSettings();
            var newWindowSettings = currentSettings with
            {
                WindowLeft = _mainWindow.Left,
                WindowTop = _mainWindow.Top,
                WindowWidth = _mainWindow.Width,
                WindowHeight = _mainWindow.Height,
                WindowMaximized = _mainWindow.WindowState == WindowState.Maximized
            };
            settingsService.SaveSettings(newWindowSettings);
        };

        var iconUri = new Uri("pack://application:,,,/KrnlAI.Desktop;component/Resources/Icons/krnl-ai-icon.png");
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Krnl-AI Desktop",
            IconSource = new BitmapImage(iconUri),
            Visibility = Visibility.Visible
        };

        var contextMenu = new System.Windows.Controls.ContextMenu();

        var openChatItem = new System.Windows.Controls.MenuItem { Header = "Abrir Chat" };
        openChatItem.Click += (s, e) => ShowMainWindow();

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "Configurações" };
        settingsItem.Click += (s, e) => ShowSettings();

        var separator1 = new System.Windows.Controls.Separator();

        var toggleAlwaysOnTopItem = new System.Windows.Controls.MenuItem { Header = "Sempre no Topo", IsCheckable = true };
        toggleAlwaysOnTopItem.Click += (s, e) =>
        {
            _isAlwaysOnTop = !_isAlwaysOnTop;
            _mainWindow?.SetAlwaysOnTop(_isAlwaysOnTop);
            _trayIcon?.ShowBalloonTip("Krnl-AI", _isAlwaysOnTop ? "Janela sempre no topo ativada" : "Janela sempre no topo desativada", BalloonIcon.Info);
        };

        var toggleAutoStartItem = new System.Windows.Controls.MenuItem { Header = "Iniciar com Windows", IsCheckable = true };
        toggleAutoStartItem.Click += (s, e) =>
        {
            SetAutoStart(toggleAutoStartItem.IsChecked);
        };

        toggleAutoStartItem.IsChecked = IsAutoStartEnabled();

        var separator2 = new System.Windows.Controls.Separator();

        var startListeningItem = new System.Windows.Controls.MenuItem { Header = "Iniciar Escuta" };
        startListeningItem.Click += async (s, e) =>
        {
            await ServiceLocator.Instance.ListeningService.StartListeningAsync();
            _trayIcon?.ShowBalloonTip("Krnl-AI", "Escuta contínua iniciada", BalloonIcon.Info);
        };

        var stopListeningItem = new System.Windows.Controls.MenuItem { Header = "Parar Escuta" };
        stopListeningItem.Click += async (s, e) =>
        {
            await ServiceLocator.Instance.ListeningService.StopListeningAsync();
            _trayIcon?.ShowBalloonTip("Krnl-AI", "Escuta contínua parada", BalloonIcon.Info);
        };

        var separator3 = new System.Windows.Controls.Separator();

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Sair" };
        exitItem.Click += (s, e) => ExitApplication();

        contextMenu.Items.Add(openChatItem);
        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(separator1);
        contextMenu.Items.Add(toggleAlwaysOnTopItem);
        contextMenu.Items.Add(toggleAutoStartItem);
        contextMenu.Items.Add(separator2);
        contextMenu.Items.Add(startListeningItem);
        contextMenu.Items.Add(stopListeningItem);
        contextMenu.Items.Add(separator3);
        contextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenu = contextMenu;
        _trayIcon.TrayMouseDoubleClick += (s, e) => ShowMainWindow();

        ShowMainWindow();

        _mainWindow.Loaded += (s, e) =>
        {
            RegisterGlobalHotkeys();
        };
    }

    private void RegisterGlobalHotkeys()
    {
        if (_mainWindow == null) return;

        try
        {
            _hotkeyService = new GlobalHotkeyService(_mainWindow);

            _hotkeyService.RegisterHotkey(
                ModifierKeys.Control | ModifierKeys.Shift,
                Key.K,
                () =>
                {
                    var listening = ServiceLocator.Instance.ListeningService;
                    if (listening.IsListening)
                    {
                        _ = listening.StopListeningAsync();
                        _trayIcon?.ShowBalloonTip("Krnl-AI", "Escuta contínua parada", BalloonIcon.Info);
                    }
                    else
                    {
                        _ = listening.StartListeningAsync();
                        _trayIcon?.ShowBalloonTip("Krnl-AI", "Escuta contínua iniciada", BalloonIcon.Info);
                    }
                }
            );

            _hotkeyService.RegisterHotkey(
                ModifierKeys.Control | ModifierKeys.Shift,
                Key.T,
                () =>
                {
                    _isAlwaysOnTop = !_isAlwaysOnTop;
                    _mainWindow?.SetAlwaysOnTop(_isAlwaysOnTop);
                    _trayIcon?.ShowBalloonTip("Krnl-AI", _isAlwaysOnTop ? "Sempre no topo: ON" : "Sempre no topo: OFF", BalloonIcon.Info);
                }
            );

            _trayIcon?.ShowBalloonTip("Krnl-AI", "Hotkeys: Ctrl+Shift+K (escuta), Ctrl+Shift+T (topo)", BalloonIcon.Info);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
        }
    }

    private bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue("KrnlAIDesktop") != null;
        }
        catch
        {
            return false;
        }
    }

    private void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            if (enable)
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue("KrnlAIDesktop", $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue("KrnlAIDesktop", false);
            }

            _trayIcon?.ShowBalloonTip("Krnl-AI", enable ? "Iniciar com Windows ativado" : "Iniciar com Windows desativado", BalloonIcon.Info);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
        }
    }

    private void EnsureMainWindow()
    {
        if (_mainWindow != null) return;
        _mainWindow = new MainWindow();
        _mainWindow.LogoutRequested += OnLogoutRequested;
    }

    private void ShowMainWindow()
    {
        EnsureMainWindow();
        _mainWindow!.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void ShowSettings()
    {
        EnsureMainWindow();
        _mainWindow!.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void OnLogoutRequested(object? sender, EventArgs e)
    {
        if (_mainWindow?.DataContext is ViewModels.MainViewModel vm)
            vm.StopHealthCheck();
        _mainWindow?.Hide();

        var settingsService = ServiceLocator.Instance.SettingsService;
        var settings = settingsService.LoadSettings();
        settings = settings with { AuthToken = null, RefreshToken = null, Username = null, IsAuthenticated = false };
        settingsService.SaveSettings(settings);

        ServiceLocator.Instance.KernelClient.SetTokens(null, null);

        var loginWindow = new LoginWindow();
        var result = loginWindow.ShowDialog();

        if (result != true)
        {
            ExitApplication();
            return;
        }

        var newSettings = settingsService.LoadSettings();
        newSettings = newSettings with { AuthToken = loginWindow.Token, RefreshToken = loginWindow.RefreshToken, Username = loginWindow.Username, IsAuthenticated = true };
        settingsService.SaveSettings(newSettings);
        ServiceLocator.Instance.KernelClient.SetTokens(loginWindow.Token, loginWindow.RefreshToken);

        _mainWindow?.Show();
    }

    private void ExitApplication()
    {
        _hotkeyService?.Dispose();
        ServiceLocator.Instance.Dispose();
        _trayIcon?.Dispose();
        Shutdown();
    }

    private bool _errorShown;

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        KrnlLogger.Write($"Unhandled: {e.Exception}");
        if (!_errorShown)
        {
            _errorShown = true;
            MessageBox.Show(e.Exception.ToString(), "KrnlAI Desktop error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        e.Handled = true;
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        KrnlLogger.Write($"Unhandled: {e.Exception.Message}");
        e.SetObserved();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Dispose();
        _trayIcon?.Dispose();
        ServiceLocator.Instance.Dispose();
        base.OnExit(e);
    }
}
