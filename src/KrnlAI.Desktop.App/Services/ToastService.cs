using Hardcodet.Wpf.TaskbarNotification;

namespace KrnlAI.Desktop.App.Services;

public sealed class ToastService
{
    private readonly TaskbarIcon _trayIcon;

    public ToastService(TaskbarIcon trayIcon)
    {
        _trayIcon = trayIcon;
    }

    public void Show(string title, string message, ToastType type = ToastType.Info)
    {
        var icon = type switch
        {
            ToastType.Success => BalloonIcon.Info,
            ToastType.Warning => BalloonIcon.Warning,
            ToastType.Error => BalloonIcon.Error,
            _ => BalloonIcon.Info
        };
        _trayIcon?.ShowBalloonTip(title, message, icon);
    }
}

public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}
